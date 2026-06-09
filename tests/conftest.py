import pytest
import requests

@pytest.fixture(scope="session")
def BASE_URL():
    return "http://localhost:8082"

"""
    Created once per test run.
    Shared across all tests.
"""
@pytest.fixture(scope="session")
def admin_session(BASE_URL):
    session = requests.Session()

    response = session.post(
        f"{BASE_URL}/api/admin/auth/session",
        json={
            "email": "admin",
            "password": "admin"
        }
    )

    assert response.status_code == 200, "Login failed as email : \"admin\" password : \"admin\""

    return session