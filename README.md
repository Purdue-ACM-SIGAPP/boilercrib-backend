# Boiler Rooms Backend

ASP.NET Core 6.0 Web API with a MongoDB database. The Expo app lives in
[boilerrooms-frontend-2425](https://github.com/Purdue-ACM-SIGAPP/boilerrooms-frontend-2425).

## Prerequisites

- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) (`dotnet --version`)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/), for the local MongoDB

## Quick start

Run these from this folder:

```
docker compose up -d   # start MongoDB on localhost:27017 (seeded on first start)
dotnet restore
dotnet run             # API on http://localhost:5128
```

To check that it works, open http://localhost:5128/swagger.

Leave `dotnet run` running while you use the frontend. Press Ctrl-C to stop the API. To stop
MongoDB, run `docker compose down`.

## Configuration (optional)

You don't need a `.env` for local development. The Development settings already point at the
Docker MongoDB. To override a setting, copy the example file:

```
cp .env.example .env
```

Values in `.env` override `appsettings.json`:

- `ConnectionStrings__DbConnection`: MongoDB connection string (default `mongodb://localhost:27017`)
- `ConnectionStrings__DatabaseName`: database name (default `test`)
- `GoogleMaps__ApiKey`: used only by the geocoding and distance endpoints. Leave it blank if you
  don't need those endpoints.

`.env` is gitignored, so don't commit real keys.

## Local database

On first start, MongoDB is seeded from `seed/seed.js` with the buildings outlined on the
frontend map, plus sample reviews and events.

To reseed, delete the volume and start again:

```
docker compose down -v && docker compose up -d
```

## Notes

The API is open: there is no JWT validation yet. `POST api/User` creates an account from a
username and password. To find its id, call `GET api/User?username=<name>`.

## Troubleshooting

- **`dotnet: command not found`**: the SDK isn't on your PATH. If it's installed in `~/.dotnet`,
  run `export PATH="$HOME/.dotnet:$PATH"`.
- **`address already in use` on 5128**: an earlier `dotnet run` is still going. Find it with
  `lsof -i :5128` and kill it.
- **Timeouts connecting to MongoDB**: Docker isn't running, or the container isn't up. Run
  `docker compose ps` to check.
