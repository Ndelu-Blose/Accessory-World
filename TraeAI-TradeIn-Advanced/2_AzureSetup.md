# 2) Azure Computer Vision — Free Tier Setup (F0)

1. Go to **Azure Portal** → **Create resource** → search **“Computer Vision”** (or “Azure AI Vision”).
2. Pricing tier: choose **F0 (Free)**.
3. After deploy, open the resource:
   - Copy **Endpoint** (looks like `https://<region>.api.cognitive.microsoft.com/` or `https://<resourcename>.cognitiveservices.azure.com/`).
   - Copy **Key** (Key1 or Key2).

Put them in `appsettings.json` (step 3 has the exact snippet).

> If you see “invalid URI” in logs, it means endpoint/key was missing or endpoint wasn’t absolute. Double‑check now.
