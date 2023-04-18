# JavaScript Linter for Multiple Websites

This C# console application scans multiple websites for JavaScript linting issues, particularly those related to incorrectly defined variable names. It uses ESLint and Node.js to check JavaScript files and generates a unique, timestamped text file containing linting errors for each website. This enables developers and administrators to efficiently identify and fix linting issues, improving code quality and maintainability across various websites.

## Key Features

- Takes user input for the directory containing websites to scan (e.g., `Z:\inetpub`).
- Identifies JavaScript files with linting errors using ESLint and Node.js.
- Generates a unique, timestamped text file containing linting errors for each website in the "linter_error_logs" folder.
- Provides a clear overview of linting issues in the console output, helping developers and administrators maintain better code quality.

## Usage

1. Ensure Node.js is installed on your system. [Download Node.js](https://nodejs.org/)
2. Install ESLint globally using the command: `npm install -g eslint`
3. Run `eslint --init` to create a default ESLint configuration file.
4. Build and run the C# console application provided in this repository.

This project streamlines the process of checking JavaScript files for linting errors across multiple websites, making it easier for developers and administrators to maintain code quality and adhere to best practices.
