import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  // 3 requests per second = 3 VUs with 1 second iteration
  vus: 3,
  duration: '5m', // Run for 5 minutes, adjust as needed
};

export default function () {
  const url = 'http://localhost:5000/gateway/axsdk-api/ocr/cccd';
  const res = http.get(url);

  check(res, {
    'is status 200': (r) => r.status === 200,
  });

  // Random sleep between 1-2 seconds
  const randomSleep = Math.random() + 1; // Random value between 1.0 and 2.0
  sleep(randomSleep);
}
