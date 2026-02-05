# Webhook Testing Endpoints

This playground project provides dummy webhook endpoints for testing the MailLedger webhook functionality.

## Available Endpoints

### 1. Success Endpoint
**POST** `http://localhost:5001/api/webhookreceiver/success`

Always returns `200 OK`. Use this to test successful webhook deliveries.

```bash
curl -X POST http://localhost:5001/api/webhookreceiver/success \
  -H "Content-Type: application/json" \
  -d '{"test": "data"}'
```

### 2. Error Endpoint
**POST** `http://localhost:5001/api/webhookreceiver/error`

Always returns `500 Internal Server Error`. Use this to test failed deliveries and retry logic.

```bash
curl -X POST http://localhost:5001/api/webhookreceiver/error \
  -H "Content-Type: application/json" \
  -d '{"test": "data"}'
```

### 3. Retry Endpoint
**POST** `http://localhost:5001/api/webhookreceiver/retry`

Returns `500` for the first 2 attempts, then `200 OK` on the 3rd attempt. Use this to test retry logic with eventual success.

```bash
curl -X POST http://localhost:5001/api/webhookreceiver/retry \
  -H "Content-Type: application/json" \
  -d '{"test": "data"}'
```

### 4. Signature Validation Endpoint
**POST** `http://localhost:5001/api/webhookreceiver/validate-signature`

Validates the `X-Webhook-Signature` header. Use this to test webhook signature generation.

```bash
curl -X POST http://localhost:5001/api/webhookreceiver/validate-signature \
  -H "Content-Type: application/json" \
  -H "X-Webhook-Signature: your-signature-here" \
  -d '{"test": "data"}'
```

### 5. View Logs
**GET** `http://localhost:5001/api/webhookreceiver/logs`

Returns all received webhooks with timestamps and payloads.

```bash
curl http://localhost:5001/api/webhookreceiver/logs
```

### 6. Clear Logs
**DELETE** `http://localhost:5001/api/webhookreceiver/logs`

Clears all webhook logs.

```bash
curl -X DELETE http://localhost:5001/api/webhookreceiver/logs
```

## Running the Playground

1. Navigate to the playground project:
   ```bash
   cd c:\dev\mailledger\mail-ledger-api\MainLedger\MailLedger.PlaygroundProject
   ```

2. Run the project:
   ```bash
   dotnet run
   ```

3. The API will start on `http://localhost:5001`

## Testing with MailLedger

1. Start the playground project (runs on port 5001)
2. Start the main MailLedger API (runs on port 5000)
3. Create a webhook endpoint in MailLedger pointing to one of the playground endpoints:
   ```
   http://localhost:5001/api/webhookreceiver/success
   ```
4. Trigger a webhook event (e.g., confirm an extraction candidate)
5. Check the logs endpoint to see received webhooks:
   ```bash
   curl http://localhost:5001/api/webhookreceiver/logs
   ```

## Example Workflow

```bash
# 1. Clear existing logs
curl -X DELETE http://localhost:5001/api/webhookreceiver/logs

# 2. Create webhook in MailLedger (via UI or API)
# URL: http://localhost:5001/api/webhookreceiver/success
# Events: CandidateConfirmed

# 3. Trigger webhook by confirming a candidate in MailLedger

# 4. View received webhooks
curl http://localhost:5001/api/webhookreceiver/logs
```

## Notes

- All logs are stored in memory and will be lost when the application restarts
- The retry endpoint resets its counter when logs are cleared
- Logs include timestamp, payload, status code, and attempt number
