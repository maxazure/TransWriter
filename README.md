# TransWriter

## Project Description

TransWriter is a Windows Forms application designed to translate text using an API. It provides a user-friendly interface for translating text and stores translation history and API keys using SQLite.

## Installation

To build and run the project, follow these steps:

1. Clone the repository:
   ```sh
   git clone https://github.com/maxazure/TransWriter.git
   cd TransWriter
   ```

2. Restore dependencies and build the project:
   ```sh
   ./build.bat
   ```

## Usage

1. Launch the application by running the executable generated in the `bin/Release` directory.
2. Use the global hotkey (Shift + Space) to show the translation form.
3. Enter the text you want to translate and click the "Translate" button.
4. The translated text will be displayed in the application and copied to the clipboard.
5. You can save your API key in the settings form.

## License

This project is licensed under the MIT License. See the `LICENSE` file for more details.
