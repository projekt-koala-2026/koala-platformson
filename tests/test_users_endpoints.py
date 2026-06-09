import pytest
import requests

BASE_URL = "http://localhost:8080"

def test_admin_getUsersEndpoint(admin_session):
    session = admin_session

    response = session.get(f"{BASE_URL}/api/admin/user/users")

    assert response.status_code == 200

    print(response.json())