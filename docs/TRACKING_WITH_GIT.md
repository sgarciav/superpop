# Summary on what to track

IDEs:
 - .idea/ - JetBrains (Rider, IntelliJ)
 - .vscode/ - Visual Studio Code
 - .csharp.dbg - C# debugger cache

 Python (for your MediaPipe scripts):
 - venv/, env/ - Virtual environments
 - __pycache__/, *.pyc - Python cache files
 - *.egg-info/ - Package metadata

 Package managers:
 - node_modules/ - npm packages
 - package-lock.json, yarn.lock - Lock files

 Summary of key Unity folders NOT to track:
 - Library/ - Rebuilt automatically
 - Temp/ - Temporary files
 - Logs/ - Log files
 - obj/, Build/ - Compiled files
 - UserSettings/ - Local editor preferences

 What you SHOULD track:
 - Assets/ - Your code and resources ✅
 - ProjectSettings/ - Project configuration ✅
 - Packages/ - Package manifest ✅
 - scripts/ - Your Python files ✅
 - docs/ - Documentation ✅
