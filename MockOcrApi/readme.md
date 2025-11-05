## This is mockup ocr api
After lunching with port 10501, you can call sample post

curl -X POST "http://localhost:5190/ocr/process" -H "Content-Type: application/json"  -d '{ "FileId": "test-file-123", "FilePath": "/path/to/test/file.pdf" }'

Call over gateway

curl -X POST "http://localhost:5000/gateway/axsdk-api/ocr/process" -H "Content-Type: application/json"  -d '{ "FileId": "test-file-123", "FilePath": "/path/to/test/file.pdf" }'

curl "http://localhost:5000/gateway/axsdk-api/ocr/cccd"
