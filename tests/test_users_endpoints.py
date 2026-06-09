import pytest
import requests

def test_admin_getUsersEndpoint(BASE_URL, admin_session):
    session = admin_session

    response = session.get(f"{BASE_URL}/api/admin/user/users")

    assert response.status_code == 200

    print(response.json())