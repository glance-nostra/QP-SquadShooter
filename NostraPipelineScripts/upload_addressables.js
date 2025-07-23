#!/usr/bin/env node

import fs from 'fs';
import path from 'path';
import FormData from 'form-data';
import fetch from 'node-fetch';
import { fileURLToPath } from 'url';

// Configuration
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const PROJECT_ROOT = path.resolve(__dirname, '../');
const CONFIG_FILE = path.join(PROJECT_ROOT, 'game_config_template.json');
const STAGE_UPLOAD_API_ENDPOINT = 'https://turbolive-staging.glance.inmobi.com/api/v2/upload-folder';
const PROD_UPLOAD_API_ENDPOINT = 'https://connect-api.glance.com/api/v2/upload-folder';
const PR_NUMBER = process.env.PR_NUMBER || 'unknown-pr';
const RESULTS_FILE = path.join(PROJECT_ROOT, 'addressables_upload_results.json');

// Get profile and targets from environment variables
const PROFILE = process.env.PROFILE || null;
const TARGETS = process.env.TARGETS ? process.env.TARGETS.split(',') : null;

// Logging helpers
const logInfo = (message) => console.log(`🔔 ${message}`);
const logSuccess = (message) => console.log(`✅ ${message}`);
const logError = (message) => console.error(`❌ ${message}`);

// Read game name from config file
let gameConfig;
try {
    if (!fs.existsSync(CONFIG_FILE)) {
        logError(`Config file not found: ${CONFIG_FILE}`);
        process.exit(1);
    }
    gameConfig = JSON.parse(fs.readFileSync(CONFIG_FILE, 'utf8'));
    if (!gameConfig.game_name) {
        logError('No game_name found in config file');
        process.exit(1);
    }
} catch (error) {
    logError(`Error reading config file: ${error.message}`);
    process.exit(1);
}

const GAME_NAME = gameConfig.game_name;
logInfo(`Using game name: ${GAME_NAME}`);

// Determine profiles to process
const profiles = PROFILE ? [PROFILE] : ['Staging', 'Production'];
logInfo(`Processing profiles: ${profiles.join(', ')}`);

// Determine targets to process
const targets = TARGETS || ['Android', 'iOS', 'WebGL', 'StandaloneOSX', 'StandaloneWindows64'];
logInfo(`Processing targets: ${targets.join(', ')}`);

// Create a dummy index.html file
function createDummyIndexHtml(directory) {
    const indexPath = path.join(directory, 'index.html');
    const content = `
<!DOCTYPE html>
<html>
<head>
    <title>QuickPlay Games Addressable Assets</title>
</head>
<body>
    <h1>QuickPlay Games Addressable Assets</h1>
</body>
</html>
`;
    fs.writeFileSync(indexPath, content);
    logInfo(`Created dummy index.html at ${indexPath}`);
}

// Helper function to get all files in a directory and its subdirectories
function getAllFiles(dir, fileList = []) {
    const files = fs.readdirSync(dir);
    
    files.forEach(file => {
        const filePath = path.join(dir, file);
        const stat = fs.statSync(filePath);
        
        if (stat.isDirectory()) {
            getAllFiles(filePath, fileList);
        } else {
            fileList.push(filePath);
        }
    });
    
    return fileList;
}

// Upload a directory to S3 - original
async function uploadToS3(sourceDir, destinationPath, profile, target) {
    try {
        // Prepare to upload all files in the directory
        logInfo(`Uploading directory: ${sourceDir} to ${destinationPath}`);
        const UPLOAD_API_ENDPOINT = profile === 'Production' ? PROD_UPLOAD_API_ENDPOINT : STAGE_UPLOAD_API_ENDPOINT;
        // Get all files in the directory and subdirectories
        const files = getAllFiles(sourceDir);
        
        if (files.length === 0) {
            logError(`No files found in directory: ${sourceDir}`);
            return null;
        }
        
        logInfo(`Found ${files.length} files to upload`);
        
        // Create form with multiple files
        const form = new FormData();
        
        // Add each file to the form
        for (const file of files) {
            // Get relative path from the source directory
            const relativePath = path.relative(sourceDir, file);
            logInfo(`Adding file to upload: ${relativePath}`);
            
            form.append('media', fs.createReadStream(file), {
                filename: relativePath,
                contentType: 'application/octet-stream'
            });
        }
        
        // Add path and preserve structure parameters
        form.append('path', destinationPath);
        form.append('preserve_structure', 'true');

        logInfo('🚀 Uploading files to S3...');
        const response = await fetch(UPLOAD_API_ENDPOINT, {
            method: 'POST',
            body: form,
            headers: form.getHeaders()
        });

        const responseText = await response.text();
        
        if (!response.ok) {
            logError(`Upload failed (${response.status}): ${responseText}`);
            return null;
        }

        // Parse response
        let responseData;
        try {
            responseData = JSON.parse(responseText);
        } catch (e) {
            logError(`Failed to parse response: ${responseText}`);
            return null;
        }
        
        let uploadUrl;
        let successCount = 0;
        let fileCount = files.length;
        
        if (responseData?.data?.results && responseData.data.results.length > 0) {
            // New format with results array
            const successfulUploads = responseData.data.results.filter(item => item.success);
            successCount = successfulUploads.length;
            
            if (successfulUploads.length > 0) {
                // Use the first CDN URL as the base URL
                uploadUrl = successfulUploads[0].cdn_url;
                const basePath = new URL(uploadUrl).pathname.split('/').slice(0, -1).join('/');
                const catalogJson = successfulUploads.find(item => item.original_name.endsWith('.json'));

                if (catalogJson) {
                    uploadUrl = catalogJson.cdn_url.replace(`/${target}/`, `/{0}/`);
                    logSuccess(`📦 Catalog JSON CDN URL (templated): ${uploadUrl}`);
                }

                logSuccess(`Uploaded ${successfulUploads.length}/${responseData.data.total_files} files successfully.`);
                logSuccess(`Base URL path: ${basePath}`);
            } else {
                logError('No successful uploads found in results');
                return null;
            }
        } else if (responseData?.data?.cdn_urls && responseData.data.cdn_urls.length > 0) {
            // Legacy format with cdn_urls array
            uploadUrl = responseData.data.cdn_urls[0];
            successCount = responseData.data.cdn_urls.length;
            logSuccess(`Uploaded ${successCount} files. Base URL: ${uploadUrl}`);
        } else if (responseData?.data?.cdn_url) {
            // Legacy format with single cdn_url
            uploadUrl = responseData.data.cdn_url;
            successCount = 1;
            logSuccess(`Upload successful: ${uploadUrl}`);
        } else {
            logError('No download URL in response');
            return null;
        }

        return {
            catalogUrl: uploadUrl,
            fileCount: fileCount,
            successCount: successCount
        };
    } catch (error) {
        logError(`Upload error: ${error.message}`);
        return null;
    }
}

// Main function to process all profiles and targets
async function processAddressables() {
    // Initialize results object
    const results = {
        game_name: GAME_NAME,
        pr: PR_NUMBER,
        timestamp: new Date().toISOString(),
        uploads: {}
    };
    
    // Process each profile and target
    for (const profile of profiles) {
        results.uploads[profile] = {};
        
        for (const target of targets) {
            const sourceDir = path.join(PROJECT_ROOT, 'ServerData', profile, 'QuickPlay', GAME_NAME, target);
            
            if (!fs.existsSync(sourceDir)) {
                logInfo(`Source directory not found: ${sourceDir}, skipping...`);
                continue;
            }
            
            // createDummyIndexHtml(sourceDir);
            
            const destinationPath = `addressables/QuickPlay/${GAME_NAME}/${target}`;
            
            // Upload to S3
            const uploadResult = await uploadToS3(sourceDir, destinationPath, profile, target);
            
            if (uploadResult) {
                const { catalogUrl, fileCount, successCount } = uploadResult;
                results.uploads[profile][target] = { 
                    catalogUrl: catalogUrl,
                    files_total: fileCount,
                    files_uploaded: successCount
                };
                logSuccess(`Successfully uploaded ${successCount}/${fileCount} files for ${profile}/${target}`);
            } else {
                logError(`Failed to upload ${profile}/${target} addressables`);
                results.uploads[profile][target] = { error: 'Upload failed' };
            }
        }
    }
    
    // Save results to file
    fs.writeFileSync(RESULTS_FILE, JSON.stringify(results, null, 2));
    logSuccess(`Results saved to ${RESULTS_FILE}`);
    
    // Return success based on whether all uploads succeeded
    const allUploadsSucceeded = Object.values(results.uploads).every(profileUploads => 
        Object.values(profileUploads).every(upload => upload.catalogUrl)
    );
    
    return allUploadsSucceeded;
}

// Run the main function
processAddressables().then(success => {
    if (success) {
        logSuccess('All addressable uploads completed successfully!');
        process.exit(0);
    } else {
        logError('Some addressable uploads failed. Check the logs for details.');
        process.exit(1);
    }
}).catch(error => {
    logError(`Unhandled error: ${error.message}`);
    process.exit(1);
});