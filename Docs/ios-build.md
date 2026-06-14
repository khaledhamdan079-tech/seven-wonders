# iOS build on Mac

Use the GitHub repo as the transfer format. Do not copy `Library`, `Temp`, `Builds`, `Logs`, or `.utmp`; Unity regenerates those on the Mac.

## Mac setup

1. Install Unity Hub.
2. Install Unity `6000.4.11f1` with iOS Build Support.
3. Install Xcode from the Mac App Store or Apple Developer.
4. Open Xcode once and accept/install its required components.
5. Sign in to Xcode with your Apple ID under Xcode > Settings > Accounts.

## Clone and open

```bash
git clone https://github.com/khaledhamdan079-tech/seven-wonders.git
cd seven-wonders
```

Open the folder in Unity Hub with Unity `6000.4.11f1`.

## Build from Unity

1. Open File > Build Profiles.
2. Add or select iOS.
3. Switch Profile.
4. Build or Build And Run.
5. Select `Builds/iOS` as the output folder.

Unity generates an Xcode project. Open `Builds/iOS/Unity-iPhone.xcodeproj`, select your team in Signing & Capabilities, connect your iPhone, then Run.

## Build from Terminal

From the project root on the Mac:

```bash
/Applications/Unity/Hub/Editor/6000.4.11f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath "$PWD" \
  -buildTarget iOS \
  -executeMethod SevenWondersDuel.Editor.IosBuild.BuildXcodeProject \
  -logFile Logs/ios-build.log
```

Then open the generated Xcode project in `Builds/iOS`.
