# Local Docker setup

Run all commands in this file from the `backend` directory.

## Configuration

Create the local environment file once:

```powershell
Copy-Item .env.example .env
```

`appsettings.json` is intended for running the project directly from the IDE. It is ignored by Git and excluded from Docker images. Containers receive the same settings through `.env` using ASP.NET's double-underscore notation, for example `ConnectionStrings__Local_Cache_Redis` maps to `ConnectionStrings:Local_Cache_Redis`.

Changing `.env` does not update an existing container. Remove and recreate that container after configuration changes.

## Shared resources

```powershell
docker network create koala_network
docker volume create koala_database_volume
docker volume create koala_backend_volume
```

Commands may report that a network or volume already exists; that is safe.

## Redis

```powershell
docker build -f koala_redis.Dockerfile -t koala_redis_image .
docker rm -f koala_redis_container 2>$null
docker run -d --name koala_redis_container --network koala_network --restart always --memory="150m" --env-file .env -p 6379:6379 koala_redis_image
```

## PostgreSQL

```powershell
docker build -f koala_postgres.Dockerfile -t koala_postgres_image .
docker rm -f koala_postgres_container 2>$null
docker run -d --name koala_postgres_container --network koala_network --restart always --memory="350m" --env-file .env -v koala_database_volume:/var/lib/postgresql/data -p 5432:5432 koala_postgres_image
```

## Test email server

```powershell
docker build -f koala_test_mailpit.Dockerfile -t koala_test_mailpit .
docker rm -f koala_test_mailpit_container 2>$null
docker run -d --name koala_test_mailpit_container --network koala_network --restart unless-stopped -p 1025:1025 -p 8025:8025 koala_test_mailpit
```

Mailpit UI is available at <http://localhost:8025>.

## Backend

```powershell
docker build -f koala_backend.Dockerfile -t koala_backend_image .
docker rm -f koala_backend_container 2>$null
docker run -d --name koala_backend_container --network koala_network --restart on-failure --memory="400m" --env-file .env -v koala_backend_volume:/app/backend_data -p 8080:8080 koala_backend_image
```

Check startup:

```powershell
docker ps --filter name=koala_backend_container
docker logs --tail 100 koala_backend_container
Invoke-WebRequest http://localhost:8080/openapi/v1.json
```

## What CI currently verifies

The `backend_unit_tests.yml` workflow runs unit tests against Entity Framework's in-memory provider. It does not start `Program.cs`, Redis, PostgreSQL, or a backend container. The Docker workflow verifies that an image can be built, but it does not run that image. A green workflow therefore does not currently prove that the complete application starts.
