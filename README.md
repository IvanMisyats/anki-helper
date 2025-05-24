# anki-helper

A simple Minimal ASP.NET Core 8 application for translating Danish words (or phrases) into English using OpenAI’s ChatGPT API and seamlessly adding them to your Anki collection via AnkiConnect.

## Features

* **Single-page UI**: Input field and button only.
* **Danish → English translation**: Uses your OpenAI API key and a custom prompt.
* **Editable translations**: Review and modify the response before adding.
* **Add to Anki**: One-click push to your Anki Desktop (via AnkiConnect) and automatic sync to AnkiWeb.
* **Lightweight**: Single container, minimal dependencies.

## Tech Stack

* **Backend**: ASP.NET Core 8 Minimal API
* **Frontend**: HTML/CSS + vanilla JavaScript (Fetch API)
* **API**: OpenAI ChatGPT via `OpenAI-DotNet` SDK
* **Anki Integration**: AnkiConnect (HTTP/JSON-RPC)
* **Containerization**: Docker

## Prerequisites

* [.NET 8 SDK](https://dotnet.microsoft.com/download)
* [Docker](https://www.docker.com/) (optional)
* **Anki Desktop** with the [AnkiConnect](https://ankiweb.net/shared/info/2055492159) add-on installed and running
* **Environment variables**:

    * `OPENAI_API_KEY`: Your OpenAI API key
    * `ANKI_CONNECT_URL`: URL for AnkiConnect (default: `http://127.0.0.1:8765`)
    * `DECK_NAME`: (Optional) Name of the Anki deck to add cards (default: `Default`)

## Installation

### 1. Clone the repository

```bash
git clone https://github.com/IvanMisyats/anki-helper.git
cd anki-helper
```

### 2. Configure environment variables

```bash
export OPENAI_API_KEY="your_openai_key"
export ANKI_CONNECT_URL="http://127.0.0.1:8765"
# Optional:
export DECK_NAME="MyDeck"
```

### 3. Run locally

```bash
dotnet run --urls http://0.0.0.0:80
```

### 4. Docker (optional)

```bash
docker build -t anki-translator .
docker run -d -p 80:80 \
  -e OPENAI_API_KEY="$OPENAI_API_KEY" \
  -e ANKI_CONNECT_URL="$ANKI_CONNECT_URL" \
  -e DECK_NAME="$DECK_NAME" \
  anki-translator
```

## Usage

1. Open your browser (mobile or desktop) at `http://<server-ip>`.
2. Enter a Danish word or phrase and click **Translate**.
3. Review/edit the returned translation in the translation field.
4. Click **Add to Anki** to push the card into your chosen deck.

## Project Structure

```text
/Program.cs          # Minimal API and endpoints
/wwwroot/            # Static files (HTML, CSS, JS)
  ├─ index.html      # Single-page UI with vanilla JS fetch logic
  ├─ style.css       # Theme
  └─ app.js          # Fetch and DOM update logic
/Dockerfile          # Container spec
```

## Contributing

Feel free to open issues or pull requests—improvements, bug fixes, and alternate integration ideas are welcome!

## License

This project is released under the MIT License. See [LICENSE](LICENSE) for details.
