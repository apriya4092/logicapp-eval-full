```markdown
# Logic App Evaluation — Customer Validation (Local)

Validate a customer’s **email** and **phone number** with a Logic App (Standard) running locally.  
The Logic App receives a JSON payload, calls a local **.NET Minimal API** for validation, and returns a combined result with an overall `status`.

## Contents
- [Architecture](#architecture)
- [Repo structure](#repo-structure)
- [Prerequisites](#prerequisites)
- [Run locally (3 terminals)](#run-locally-3-terminals)
- [Get a signed callback URL](#get-a-signed-callback-url)
- [Test (end-to-end)](#test-end-to-end)
- [.NET validation library (evidence)](#net-validation-library-evidence)
- [Troubleshooting](#troubleshooting)
- [Notes for reviewers](#notes-for-reviewers)
- [Submission checklist](#submission-checklist)

---

## Architecture

```

curl/Postman → Logic App (Standard, local host)
├─ Parse JSON
├─ HTTP POST → [http://localhost:7072/api/ValidateEmail](http://localhost:7072/api/ValidateEmail)
├─ HTTP POST → [http://localhost:7072/api/ValidatePhoneNumber](http://localhost:7072/api/ValidatePhoneNumber)
└─ HTTP Response (200 Success / 400 Error)

````

**Input JSON**
```json
{ "customerId": "string", "email": "string", "phoneNumber": "string" }
````

**Output JSON**

```json
{
  "customerId": "string",
  "emailValidation": { "IsValid": true,  "Message": "string" },
  "phoneValidation": { "IsValid": true,  "Message": "string" },
  "status": "Success" | "Error"
}
```

---

## Repo structure

```
logicapp-eval-full/
├─ evidence/
│  ├─ func-start.log
│  ├─ callback-url-REDacted.txt
│  ├─ sample-success.json
│  ├─ sample-failure.json
│  └─ versions.txt
├─ logicapp/
│  ├─ Artifacts/
│  │  └─ CustomerValidation.dll
│  ├─ workflows/
│  │  ├─ health/
│  │  │  └─ workflow.json
│  │  └─ validateCustomer/
│  │     └─ workflow.json
│  ├─ connections.json
│  ├─ host.json
│  ├─ local.settings.json
│  └─ package.json
├─ src/
│  └─ CustomerValidation/
│     ├─ CustomerValidation.csproj
│     └─ Validator.cs
├─ test/
│  ├─ sample-request.json
│  ├─ sample-bad-email.json
│  └─ sample-bad-phone.json
└─ validator-api/
   ├─ Program.cs
   ├─ ValidatorApi.csproj
   ├─ appsettings.json
   └─ appsettings.Development.json

```

---

## Prerequisites

* **.NET 6 SDK** (or newer that supports `net6.0`)
* **Azure Functions Core Tools v4**
* **Azurite** (local Storage emulator)
* **Visual Studio Code** (optional, recommended)

> Default Logic App host port is **7071**. In this project we often used **7091**; both work.

---

## Run locally (3 terminals)

### Terminal A — Azurite

```bash
azurite --silent
```

### Terminal B — Logic App host

```bash
cd logicapp-eval-full/logicapp
# use 7071 if it's free; 7091 is fine too
func start --port 7091
```

You should see:

```
Functions:
  health:           http://localhost:7091/api/health/triggers/When_an_HTTP_request_is_received/invoke
  validateCustomer: http://localhost:7091/api/validateCustomer/triggers/When_an_HTTP_request_is_received/invoke
```

### Terminal C — Validator API

```bash
cd logicapp-eval-full/validator-api
dotnet run --urls http://localhost:7072
# Now listening on: http://localhost:7072
```

---

## Get a signed callback URL

The trigger must be invoked with a **signed** URL (includes `api-version`, `sp`, `sv`, and `sig`).
Fetch it from the local management endpoint (works without opening the Designer):

> Replace `PORT=7091` if you run the host on another port.

```bash
PORT=7091 WF=validateCustomer TRIGGER=When_an_HTTP_request_is_received
CALLBACK=$(curl -sS -X POST \
  -H "Content-Type: application/json" \
  -d '{}' \
  "http://localhost:$PORT/runtime/webhooks/workflow/api/management/workflows/$WF/triggers/$TRIGGER/listCallbackUrl?api-version=2016-06-01" \
  | python3 -c 'import sys,json; print(json.load(sys.stdin)["value"])')

echo "$CALLBACK"
```

---

## Test (end-to-end)

```bash
# success (expect HTTP 200)
curl -i -sS -X POST "$CALLBACK" -H "Content-Type: application/json" -d '{
  "customerId":"12345",
  "email":"john.doe@example.com",
  "phoneNumber":"123-456-7890"
}'

# failure (expect HTTP 400)
curl -i -sS -X POST "$CALLBACK" -H "Content-Type: application/json" -d '{
  "customerId":"12345",
  "email":"john.doe@example.com",
  "phoneNumber":"1234567"
}'
```

**Accepted phone formats:** `123-456-7890`, `(123)456-7890`, `(123) 456-7890`
**Email rule:** simple RFC-like regex (single `@`, dot in domain)

---

## .NET validation library (evidence)

* **Source:** `src/CustomerValidation/Validator.cs`

  * `Validator.ValidateEmail(string) → ValidationResult`
  * `Validator.ValidatePhoneNumber(string) → ValidationResult`
* **Built DLL:** `logicapp/Artifacts/CustomerValidation.dll` (evidence for the .NET requirement)

The Logic App calls the same logic via HTTP (Minimal API at `http://localhost:7072`), which is robust on macOS.
On Windows or macOS Ventura+, you can switch to **“Call a local function in this logic app”** using the same library if desired.

### Rebuild the DLL (optional)

```bash
cd logicapp-eval-full/src/CustomerValidation
dotnet build -c Release
cp bin/Release/net6.0/CustomerValidation.dll ../../logicapp/Artifacts/
```

---

## Troubleshooting

* **401 Unauthorized when invoking trigger**
  You used the base URL. Use the **signed** URL from `listCallbackUrl`.

* **400 MissingApiVersionParameter**
  Your URL missed `?api-version=...`. Use the signed URL (it includes it).

* **“Unsupported media type.”** (when asking for callback URL)
  Add `-H "Content-Type: application/json" -d '{}'` to the `listCallbackUrl` POST.

* **Port 7071 unavailable**
  Start the host on another port: `func start --port 7091` and use that port in `listCallbackUrl`.

* **CORS in browser**
  Use curl/Postman or serve a simple test page from the validator API.

---

## Notes for reviewers

* The exam’s “Execute .NET Function / Call a local function in this logic app” path can be unreliable on **macOS 11** for local dev (custom-code worker/bundle errors).
  To keep the solution **runnable and equivalent**, the .NET validation is hosted as a local API and invoked via HTTP actions.
* Evidence included:

  * DLL under `logicapp/Artifacts/CustomerValidation.dll`
  * Source under `src/CustomerValidation/`
  * `evidence/` with signed URL (redacted) and sample responses

---

