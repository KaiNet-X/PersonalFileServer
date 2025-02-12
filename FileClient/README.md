# FileClient documentation

FileClient is a cross-platform GUI built to connect to FileServer. It allows you to securely transfer files between different devices.

Features
---
---
- **Users**: In order to authenticate with the server, you need to create a user. 
The password is not required, but choosing a safe one helps to ensure no one gains unauthorized to your files and keeps your encryption key safe.
The second hash of your password is sent to the server to authenticate against.
- **Encryption**: Your files are encrypted by the client before being sent to the server. 
As such, the encryption key is based on the hash of your password. 
This allows each instance of the client you are signed in to access to your files, while keeping the contents inaccessible to the server.
Because of this, a strong password is recommended to keep your data secure.
Note that the encryption key is stored in a file client side, so keep it secure. In the future, I intend to make this behavior configurable.
- **Directory storage**: The client allows you to upload directory structures which are preserved on the server. 
You can then download the directory and it will be preserved as it was when uploaded.
- **Drag&drop**: Drag files or folders onto the file list pane to upload them quickly. 
Note that this feature currently doesn't work on most linux distributions.

Limitations
---
---
- The FileClient currently doesn't work on mobile devices

Future improvements
---
---
- Implement mobile views, build for Android and IOS
- Runnable builds for each platform
- Find a more secure way to store the user file
- Fix bugs
- Dark and light theme support
- Switch between users easily

How to run
---
---
Currently, you need to build from source in order to run this. The dotnet8 SDK is required to build it.
Open terminal, move to the FileClient directory, and run `dotnet run`.