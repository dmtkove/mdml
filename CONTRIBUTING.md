# Contributing to mdml

Thanks for helping. mdml is licensed under the [GNU GPL v3](LICENSE). By opening a pull request you agree that your contribution is licensed under the same terms.

## Development setup

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download).
2. Clone the repository:

   ```bash
   git clone https://github.com/dmtkove/mdml.git
   cd mdml
   ```

3. Run the tests:

   ```bash
   dotnet test Mdml.slnx
   ```

4. Run the CLI from source:

   ```bash
   dotnet run --project src/Mdml.Cli -- --help
   ```

On macOS, `swiftc` and `pkgbuild` are needed only if you build the `.pkg` installer (`./packaging/build.sh`).

## Project conventions

- Keep conversion logic in `Mdml.Core`. The CLI should stay a thin argument layer.
- Do not add a server, cloud API, or browser engine to the converter.
- Themes are HTML files with `{{title}}`, `{{content}}`, `{{css}}`, and optional `{{scripts}}`. Do not invent a plugin format.
- New behavior should come with tests in `tests/Mdml.Core.Tests` or `tests/Mdml.Cli.Tests`.
- Match the existing C# style: nullable reference types, implicit usings, no warnings.

## Pull requests

1. Create a branch from `main`.
2. Make a focused change (one concern per PR when you can).
3. Run `dotnet test Mdml.slnx`.
4. Open a pull request that says **why** the change exists, not only what you edited.

Please do not commit `artifacts/`, `bin/`, `obj/`, or personal IDE settings.

## Reporting issues

Use [GitHub Issues](https://github.com/dmtkove/mdml/issues). Include the OS, how you installed mdml, the command you ran, and the Markdown that failed (trimmed if it is long).

Security reports are covered in [SECURITY.md](SECURITY.md).
