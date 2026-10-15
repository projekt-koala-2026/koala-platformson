# User Endpoints
---

## `POST api/koala/account/users`

```mermaid
flowchart TD
    A["POST /api/koala/account/users"]

    B{"Is the request valid?"}
    C[[Call service]]
    R1((400 Bad Request))

    D{"Is the user authenticated?"}
    E{"Is the user an ORGANIZATION_ADMIN?"}
    R2((403 Forbidden))

    F{{Allow only user roles beginning with ORGANIZATION_}}
    G{{Allow only user roles beginning with TEAM_}}

    H["Check database for a user with the provided email"]
    I{"Does the user exist?"}

    J{"Is the user verified?"}
    R3((402 Bad request))

    K["Create user with the provided roles and generate a verification link; invalidate the previous link"]
    L["Create user with the provided roles and generate a verification link"]

    M["Send verification link to the provided email"]
    R4((Success))

    A --> B
    B -->|Yes| C
    B -->|No| R1

    C --> D
    D -->|Yes| E
    D -->|No| G

    E -->|Yes| F
    E -->|No| R2

    F --> H
    G --> H

    H --> I
    I -->|Yes| J
    I -->|No| L

    J -->|Yes| R3
    J -->|No| K

    K --> M
    L --> M
    M --> R4

```
---

## `GET api/koala/account/users{id}`

```mermaid
flowchart TD
    A["GET /api/koala/account/users/{id}"]

    B{"Is the request valid?"}
    C[["Call service"]]
    D{"Is the user authenticated?"}
    E{"Does the user have the ORGANIZATION_ADMIN role?"}
    F{"Is the authenticated user ID the same as {id}?"}
    G["Check whether the target user exists"]
    H{"Does the user exist?"}
    I["Return user data"]

    R1((400 Bad Request))
    R2((401 Unauthorized))
    R3((403 Forbidden))
    R4((404 Not Found))
    R5((200 OK))

    A --> B

    B -->|No| R1
    B -->|Yes| C

    C --> D

    D -->|No| R2
    D -->|Yes| E

    E -->|Yes| G
    E -->|No| F

    F -->|No| R3
    F -->|Yes| G

    G --> H

    H -->|No| R4
    H -->|Yes| I

    I --> R5
```
---

## `GET api/koala/account/users`

```mermaid
flowchart TD
    A["GET /api/koala/account/users"]

    B{"Is the request valid?"}
    C[[Call service]]
    D{"Is the user authenticated?"}
    E{"Does the user have the ORGANIZATION_ADMIN role?"}
    F["Get filtered users with pagination"]
    G["Return user data"]

    R1((400 Bad Request))
    R2((401 Unauthorized))
    R3((403 Forbidden))
    R4((200 OK))

    A --> B

    B -->|No| R1
    B -->|Yes| C

    C --> D

    D -->|No| R2
    D -->|Yes| E

    E -->|No| R3
    E -->|Yes| F

    F --> G
    G --> R4

```
---

## `PUT api/koala/account/users{id}`

```mermaid
flowchart TD
    A["PUT /api/koala/account/users/{id}"]

    B{"Is the request valid?"}
    C[[Call service]]
    R1((400 Bad Request))

    D{"Is the user authenticated?"}
    E{"Does the user have the ORGANIZATION_ADMIN role?"}
    R2((401 Unauthorized))

    F{"Is the authenticated user ID the same as {id}?"}
    R3((403 Forbidden))

    G["Check whether the target user exists"]
    H{"Does the user exist?"}
    R4((404 Not Found))

    I["Update user"]
    J["Return updated user"]
    R5((200 OK))

    A --> B

    B -->|Yes| C
    B -->|No| R1

    C --> D

    D -->|No| R2
    D -->|Yes| E

    E -->|Yes| G
    E -->|No| F

    F -->|Yes| G
    F -->|No| R3

    G --> H

    H -->|Yes| I
    H -->|No| R4

    I --> J
    J --> R5

```

---

## `DELETE api/koala/account/users{id}`

```mermaid
flowchart TD
    A["DELETE /api/koala/account/users/{id}"]

    B{"Is the request valid?"}
    C[["Call service"]]
    D{"Is the user authenticated?"}
    E{"Does the user have the ORGANIZATION_ADMIN role?"}
    F{"Is the authenticated user ID the same as {id}?"}
    G["Check whether the target user exists"]
    H{"Does the user exist?"}
    I["Delete user"]

    R1((400 Bad Request))
    R2((401 Unauthorized))
    R3((403 Forbidden))
    R4((404 Not Found))
    R5((200 Ok))

    A --> B

    B -->|No| R1
    B -->|Yes| C

    C --> D

    D -->|No| R2
    D -->|Yes| E

    E -->|Yes| G
    E -->|No| F

    F -->|No| R3
    F -->|Yes| G

    G --> H

    H -->|No| R4
    H -->|Yes| I

    I --> R5
```
---