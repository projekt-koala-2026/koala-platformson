# Docker Setup

## Create custom isolated bridge network so containers communicate by container name
```docker file
docker network create koala_network
```

## Create named volumes for database and backend data persistence across container restarts
```dockerfile
docker volume create koala_database_volume
```
```dockerfile
docker volume create koala_backend_volume
```


# Redis Setup

## Build Redis Image
```dockerfile
docker build -f koala_redis.Dockerfile -t koala_redis_image .
```

## Launch Redis Container (Memory restricted to 150MB)
```dockerfile
docker run -d \
  --name koala_redis_container \
  --network koala_network \
  --restart always \
  --memory="150m" \
  --env-file .env \
  -p 6379:6379 \
  koala_redis_image
```

```dockerfile
docker run -d `
  --name koala_redis_container `
  --network koala_network `
  --restart always `
  --memory="150m" `
  --env-file .env `
  -p 6379:6379 `
  koala_redis_image
```

# Postgres setup

## Build Postgres Image
```dockerfile
docker build -f koala_postgres.Dockerfile -t koala_postgres_image .
```

## Launch Postgres Container (Memory restricted to 350MB)
```dockerfile
docker run -d \
  --name koala_postgres_container \
  --network koala_network \
  --restart always \
  --memory="350m" \
  --env-file .env \
  -v koala_database_volume:/var/lib/postgresql/data \
  -p 5432:5432 \
  koala_postgres_image
```

```dockerfile
docker run -d `
  --name koala_postgres_container `
  --network koala_network `
  --restart always `
  --memory="350m" `
  --env-file .env `
  -v koala_database_volume:/var/lib/postgresql/data `
  -p 5432:5432 `
  koala_postgres_image
```

# Backend Setup

## Build Backend Image
```dockerfile
docker build -f koala_backend.Dockerfile -t koala_backend_image .
```

TODO: change the volume to somwhere meaningful XDD
## Launch Backend Container (Memory restricted to 400MB)
```dockerfile
docker run -d \
  --name koala_backend_container \
  --network koala_network \
  --restart on-failure \
  --memory="400m" \
  --env-file .env \
  -v koala_backend_volume:/app/public_files/ \
  -p 8080:8080 \
  koala_backend_image
```

```dockerfile
docker run -d `
  --name koala_backend_container `
  --network koala_network `
  --restart on-failure `
  --memory="400m" `
  --env-file .env `
  -v koala_backend_volume:/app/public_files/ `
  -p 8080:8080 `
  koala_backend_image
```

# Test Email Server Setup

## Build Test Email Server Image
```dockerfile
docker build -f koala_test_mailpit.Dockerfile -t koala_test_mailpit .
```

## Launch Test Email Server Container
```dockerfile
docker run -d --name koala_test_mailpit_container --network koala_network -p 1025:1025 -p 8025:8025 `
  koala_test_mailpit 
```