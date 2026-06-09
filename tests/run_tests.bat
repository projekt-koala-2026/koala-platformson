@echo off

echo Ensuring fresh koala-container for tests...
docker-compose -f docker-compose.test.yml down -v

echo Docker-compose Running...
docker-compose -f docker-compose.test.yml up -d --build


:wait
curl -s -S -f http://localhost:8082/health > nul
if errorlevel 1 (
    echo Backend not ready yet, retrying...
    timeout /t 2 > nul
    goto wait
)

echo System Ready

echo Running tests...
pytest -v -s

echo Deleting test container
docker-compose -f docker-compose.test.yml down -v

echo Done
pause