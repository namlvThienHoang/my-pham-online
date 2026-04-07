import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');

export const options = {
  // Smoke test: low load to verify system is working
  stages: [
    { duration: '30s', target: 10 },  // Ramp up to 10 req/s
    { duration: '1m', target: 10 },   // Stay at 10 req/s
    { duration: '30s', target: 0 },   // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests should complete below 500ms
    errors: ['rate<0.1'],             // Error rate should be less than 10%
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000/api/v1';

export default function () {
  // Test 1: Health Check
  const healthRes = http.get(`${BASE_URL}/health/ready`);
  check(healthRes, {
    'health check status is 200': (r) => r.status === 200,
  });
  errorRate.add(healthRes.status !== 200);
  sleep(1);

  // Test 2: Get Products (empty list is OK for smoke test)
  const productsRes = http.get(`${BASE_URL}/products?pageSize=5`);
  check(productsRes, {
    'products status is 200': (r) => r.status === 200,
    'products response has items array': (r) => JSON.parse(r.body).items !== undefined,
  });
  errorRate.add(productsRes.status !== 200);
  sleep(1);

  // Test 3: Product Suggest
  const suggestRes = http.get(`${BASE_URL}/products/suggest?q=son`);
  check(suggestRes, {
    'suggest status is 200': (r) => r.status === 200,
  });
  errorRate.add(suggestRes.status !== 200);
  sleep(1);

  // Test 4: Get Cart (requires auth in production, skip for smoke)
  // const cartRes = http.get(`${BASE_URL}/carts/me`, {
  //   headers: { Authorization: 'Bearer token' }
  // });

  console.log('Smoke test completed');
}

export function handleSummary(data) {
  return {
    'stdout': textSummary(data, { indent: ' ', enableColors: true }),
    'tests/k6/results/smoke-summary.json': JSON.stringify(data),
  };
}

function textSummary(data, options) {
  return `\n=== K6 Smoke Test Summary ===
Execution Time: ${new Date(data.state.testRunDurationMs).toISOString().substr(11, 8)}
Requests: ${data.metrics.http_reqs.values.count}
Failed Requests: ${data.metrics.http_req_failed.values.rate * 100}%
Avg Duration: ${data.metrics.http_req_duration.values.avg.toFixed(2)}ms
P95 Duration: ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)}ms
Error Rate: ${data.metrics.errors.values.rate * 100}%
=================================\n`;
}
