# Security

If you find a vulnerability in mdml, please **do not** open a public issue.

Use GitHub’s [private vulnerability reporting](https://github.com/dmtkove/mdml/security/advisories/new) for this repository, or email the maintainer through the address on their [GitHub profile](https://github.com/dmtkove).

Include:

- What the issue is
- How to reproduce it
- Versions / OS you tested

Please give a reasonable window to fix and release before public disclosure.

mdml runs locally and does not send your Markdown to a server. Generated HTML may load Mermaid from a CDN when a document contains diagrams; treat that as a viewing-time network request, not as data upload of the source file.
