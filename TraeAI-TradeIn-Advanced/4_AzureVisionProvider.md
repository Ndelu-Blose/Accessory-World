# 4) AzureVisionAssessmentProvider.cs (drop‑in)

This provider uses **Azure AI Vision (ImageAnalysis)** to extract **captions, tags** and simple **condition cues** (e.g., cracked, scratched). It then maps to a coarse grade (**A/B/C/D**) you can refine later.

See the ready file in `/CodeSnippets/AzureVisionAssessmentProvider.cs`. Adapt interface names if your project differs (e.g., `IDeviceAssessmentProvider`, `DeviceAssessmentRequest`, `DeviceAssessmentResult`).

**Important**: You must pass **absolute** image URLs or load images from local file/stream. If you store relative paths like `/uploads/...`, generate full URLs before calling the provider or open the file stream from disk.
