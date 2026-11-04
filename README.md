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
cp .env.example .env   # then set JWT_KEY in .env
dotnet restore
dotnet run             # API on http://localhost:5128
```

To check that it works, open http://localhost:5128/scalar.

Leave `dotnet run` running while you use the frontend. Press Ctrl-C to stop the API. To stop
MongoDB, run `docker compose down`.

## Configuration

The only required setting is `JWT_KEY`. The Development settings already point at the Docker
MongoDB. Copy the example file and set `JWT_KEY`:

```
cp .env.example .env
```

Values in `.env` override `appsettings.json`:

- `JWT_KEY`: required. Signing key for login tokens, at least 64 characters, for example the
  output of `openssl rand -base64 64`.
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

## Authentication

`POST api/User` (sign up) and `POST api/User/login` return `{ token, user }`. Send the token as
`Authorization: Bearer <token>` on requests that change data or read accounts. Reading
buildings, reviews and events needs no token.

Users can change only their own account, reviews and events. Admins (`accountType` 4) can change
any of them, manage buildings, and list users. To make the first admin, set it in MongoDB:

```
db.user.updateOne({ username: "<name>" }, { $set: { accountType: 4 } })
```

Accounts created before passwords were hashed get a 403 from login until they choose a new
password with `POST api/User/reset-password` and `{ username, password, newPassword }`.

Sign-up, login and password reset are limited to 10 requests per minute per IP address.

## Troubleshooting

- **`dotnet: command not found`**: the SDK isn't on your PATH. If it's installed in `~/.dotnet`,
  run `export PATH="$HOME/.dotnet:$PATH"`.
- **`address already in use` on 5128**: an earlier `dotnet run` is still going. Find it with
  `lsof -i :5128` and kill it.
- **Timeouts connecting to MongoDB**: Docker isn't running, or the container isn't up. Run
  `docker compose ps` to check.
