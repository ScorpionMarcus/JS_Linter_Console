# Server JavaScript Linter

Server JavaScript Linter is a C# console application that automatically scans all websites located on a server and checks their JavaScript files for linting errors, particularly those related to incorrectly defined variable names. The application leverages ESLint through Node.js to perform the linting process and generates a unique text file containing linting errors for each individual website. This allows developers and administrators to identify and fix linting issues more efficiently, ensuring better code quality and maintainability across multiple websites hosted on the same server.

## Key Features

- Scans all websites located under a specified directory (e.g., D:\inetpub).
- Identifies JavaScript files with linting errors using ESLint and Node.js.
- Generates a unique text file containing linting errors for each website.
- Provides a clear overview of linting issues to help developers and administrators maintain better code quality.

## Usage

1. Ensure Node.js is installed on your system. [Download Node.js](https://nodejs.org/)
2. Install ESLint globally using the command: `npm install -g eslint`
3. Run `eslint --init` to create a default ESLint configuration file.
4. Build and run the C# console application provided in this repository.

This project simplifies the process of checking JavaScript files for linting errors across multiple websites on a server, making it easier for developers and administrators to maintain code quality and adhere to best practices.
