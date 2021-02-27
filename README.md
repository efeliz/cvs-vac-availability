# cvs-vac-availability
A quick web scraper that checks CVS's website for Covid-19 vaccine availability changes and notifies by email via SendGrid's API.

# Setup Instructions
### Email Sending Setup:
1. Head over to SendGrid and signup for a free account (this is used to send emails): https://sendgrid.com/solutions/email-api/
2. Create an API Key after you've made an account (be sure to save this key somewhere since you can't see it again after first making it)

### How to Run Application (easier way)
1. Head over to releases section (https://github.com/efeliz/cvs-vac-availability/releases) and download either `win-x64.zip` (for Windows) or `osx-x64.zip` (for Mac)
2. Unzip the downloaded file
3. On Windows double-click the `CVS-Vac-Fetcher.exe` file to run and follow the prompts on-screen.
4. On Mac open terminal -> `cd` to the unzipped folder -> enter command `sudo ./CVS-Vac-Fetcher` to run the program -> follow prompts on-screen. 

### How to Run Application (less easy way - for developers)
1. Download source code by either cloning, forking, or selecting "Source Code" on the releases page (https://github.com/efeliz/cvs-vac-availability/releases)
2. Head over to the Microsoft .NET SDK download page and select the **SDK** (at least version 5.0) that matches your system (Windows, Mac, etc.): https://dotnet.microsoft.com/download
3. Open Terminal (or Powershell) and `cd` to your project directory
4. Enter the command `dotnet build` followed by `dotnet run`
5. Optionally, you could just download Visual Studio 2019 on Windows or Visual Studio for Mac (not VS code): https://visualstudio.microsoft.com/

Hope this helps anyone who might need it, feel free to contact me at: estevanjfeliz@gmail.com with any questions.
