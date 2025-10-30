import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    
  // No duration needed as iterations will control the test length
};

export default function () {
  const url = 'http://localhost:5000/gateway/axsdk-api/ocr/cccd';
  const res = http.get(url);

  check(res, {
    'is status 200': (r) => r.status === 200,
  });

  sleep(1); // Simulate some user think time
}
