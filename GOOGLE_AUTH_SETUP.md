# CandyCraze Google Authentication Setup Guide

## Overview
This guide walks you through setting up Google Sign-In for CandyCraze using Google Play Games plugin.

---

## Part 1: Google Cloud Console Setup

### 1.1 Create Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Click the **project dropdown** (top-left)
3. Click **"New Project"**
4. Enter name: `CandyCraze`
5. Click **Create** and wait for it to be created
6. Select the new project from the dropdown

### 1.2 Enable Google+ API

1. In the search bar, type **"Google+ API"**
2. Click on it in the results
3. Click **"Enable"**

### 1.3 Create OAuth Consent Screen

1. Go to **"APIs & Services"** → **"Credentials"** (left sidebar)
2. If prompted to set up OAuth consent, click **"Configure Consent Screen"**
3. Choose **"External"** → Click **"Create"**
4. Fill in the form:
   - **App name:** CandyCraze
   - **User support email:** your email
   - **Developer contact information:** your email
5. Click **"Save and Continue"** through all sections
6. You're done with consent screen

### 1.4 Create Android OAuth Credential

1. Go to **"Credentials"** → Click **"+ Create Credentials"** → **"OAuth Client ID"**
2. Select **"Android"**
3. Fill in:
   - **Package name:** `com.YourCompany.CandyCraze`
   - **SHA-1 certificate fingerprint:** (see Step 2.2 below to get this)
4. Click **"Create"**
5. **Copy your Client ID** — you'll need this for Unity

> **Note:** You can update the SHA-1 later. For now, use a placeholder and update it once you have the real one.

---

## Part 2: Unity Setup

### 2.1 Install Google Play Games Plugin

1. Download the plugin from [GitHub](https://github.com/playgameservices/play-games-plugin-for-unity/releases)
   - Download the latest `.unitypackage` file
2. In Unity, open your CandyCraze project
3. Go to **Assets** → **Import Package** → **Custom Package**
4. Select the downloaded `.unitypackage`
5. Click **Import All**
6. Wait for import to complete

### 2.2 Get SHA-1 Fingerprint

#### Option A: Using Debug Keystore (Development)

1. Open **PowerShell** and run:
   ```powershell
   keytool -list -v -keystore "C:\Users\sandy\.android\debug.keystore" -alias androiddebugkey -storepass android -keypass android
   ```
   - If the file doesn't exist, Unity will create it when you build for Android
   - Look for the **SHA1** fingerprint (looks like: `AA:BB:CC:DD:...`)

#### Option B: Using Custom Keystore

1. In Unity: **File** → **Build Settings**
2. Select **Android** platform
3. Click **Player Settings** (bottom-left)
4. Go to **Publishing Settings** → **Keystore Manager**
5. Create or select a keystore
6. Use keytool to get the SHA-1 of your keystore

### 2.3 Configure Google Play Games Plugin in Unity

1. After importing the plugin, go to **Window** → **Google Play Games** → **Setup Android**
2. A dialog will appear asking for:
   - **Google Play Games App ID:** (leave blank if you don't have it yet)
   - **Web Client ID:** (leave blank)
   - **Package name:** `com.YourCompany.CandyCraze`
3. Click **Setup**

### 2.4 Update Google Cloud with Real SHA-1

1. Get your real SHA-1 fingerprint from Step 2.2
2. Go back to [Google Cloud Console](https://console.cloud.google.com/)
3. **Credentials** → Find your Android OAuth Client ID
4. Click **Edit** (pencil icon)
5. Replace the SHA-1 with your real one
6. Click **Save**

---

## Part 3: Add Scripts to Your Unity Project

Three scripts have been created for you:

### Scripts to Add:

1. **GoogleAuthManager.cs** → `Assets/Scripts/Managers/`
   - Handles Google authentication
   - Manages login/logout
   - Extracts user info (email, name, ID token)

2. **CloudSaveManager.cs** → `Assets/Scripts/Managers/`
   - Handles cloud save/load
   - **Only works online** (checks network reachability)
   - Uploads/downloads to your backend: `https://candycraze.onrender.com`

3. **LoginUI.cs** → `Assets/Scripts/UI/`
   - Manages login UI panel
   - Shows profile icon when logged in
   - Displays network status (online/offline)

### Setup in Unity:

1. Create empty GameObjects for the managers:
   - Create → Empty → Rename to `GoogleAuthManager`
   - Create → Empty → Rename to `CloudSaveManager`

2. Add scripts to GameObjects:
   - Select `GoogleAuthManager` object → Add Component → GoogleAuthManager.cs
   - Select `CloudSaveManager` object → Add Component → CloudSaveManager.cs

3. Create UI for login:
   - In your home scene, add UI:
     - Canvas → Button (Profile Icon, top-right)
     - Canvas → Panel (Login Panel, hidden by default)
     - Inside Panel: Text (Status), Button (Sign In), Button (Sign Out)

4. Attach LoginUI.cs to a UI manager:
   - Create → Empty → Rename to `LoginUIManager`
   - Add Component → LoginUI.cs
   - Drag the UI elements into the Inspector fields

---

## Part 4: Test in Android Build

### 4.1 Build for Android

1. **File** → **Build Settings**
2. Select **Android** platform
3. Click **Player Settings**:
   - **Company Name:** Your Company
   - **Product Name:** CandyCraze
   - **Bundle Identifier:** `com.YourCompany.CandyCraze` (same as Google Cloud)
4. Go to **"Resolution and Presentation"** → Change to **Portrait**
5. Click **Build and Run** (or **Build**)

### 4.2 Test Authentication

1. After app launches on device:
   - Tap the profile icon (top-right)
   - Tap "Sign In with Google"
   - A Google login dialog should appear
   - Sign in with your Google account
   - After successful login, you should see your profile name

### 4.3 Test Cloud Save

1. In your game code, call:
   ```csharp
   // Upload save
   CloudSaveManager.Instance.UploadSave(jsonSaveData);

   // Download save
   CloudSaveManager.Instance.DownloadSave();
   ```

2. Listen to events:
   ```csharp
   CloudSaveManager.Instance.OnSaveComplete += (success, msg) => {
       Debug.Log($"Save: {success} - {msg}");
   };
   ```

---

## Troubleshooting

### Issue: "Sign in failed" or "Invalid Client ID"
- **Solution:** Verify SHA-1 matches in Google Cloud Console
- Go to Credentials → Edit OAuth Client → Check SHA-1 is correct

### Issue: "Network error: 401 Unauthorized"
- **Solution:** Token not being sent to backend
- Check that `GoogleAuthManager.GetAuthToken()` returns a non-null value
- Verify token format in `CloudSaveManager` (should be "Bearer TOKEN")

### Issue: "Save offline" appears but never syncs
- **Solution:** Network reachability check might be blocking
- Put app in background, turn on WiFi/mobile data
- Trigger save again or implement auto-retry logic

### Issue: Plugin not found or "Google Play Games not imported"
- **Solution:** Reimport the plugin
- Delete the plugin folder if it exists: `Assets/GooglePlayGames/`
- Reimport the .unitypackage

---

## Summary

✅ **What You've Done:**
- Created Google Cloud project
- Generated OAuth credentials for Android
- Got SHA-1 fingerprint from keystore
- Installed Google Play Games plugin in Unity
- Added GoogleAuthManager, CloudSaveManager, and LoginUI scripts

✅ **What Happens on App Launch:**
- GoogleAuthManager auto-initializes (tries silent sign-in)
- If already logged in: shows profile icon
- If not: shows login button
- CloudSaveManager only works when device is online

✅ **Backend Ready:**
- Your backend is live at `https://candycraze.onrender.com/`
- Handles token verification and cloud save storage

---

## Next Steps

1. **Complete the Google Cloud setup** (SHA-1, credentials)
2. **Import plugin and scripts into Unity**
3. **Create UI elements** (profile icon, login panel)
4. **Build for Android** and test on device
5. **Call `CloudSaveManager.UploadSave()` in your game code** when user finishes a level

---

## Files Created

- `GoogleAuthManager.cs` — Authentication manager
- `CloudSaveManager.cs` — Cloud save/load manager
- `LoginUI.cs` — UI manager for login
- `GOOGLE_AUTH_SETUP.md` — This guide

Good luck! 🚀
