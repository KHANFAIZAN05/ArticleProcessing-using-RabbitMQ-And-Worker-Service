# Copilot Instructions

## Project Guidelines
- For ArticleProcessing, keep article submissions in an in-memory queue and have a background worker consume that queue to save articles to the database; do not use SQL Server as the queue.
- For ArticleProcessing, do not introduce named pipes. The standalone Worker should read articles directly from a shared queue.