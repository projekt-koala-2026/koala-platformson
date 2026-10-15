# Link Endpoints
---

## `POST api/koala/account/links/register/{token}`

```mermaid
flowchart TD
    R1((Success))
    R2((402 Invalida data))
    R3((404 link does not exist or expired))
    R4((400? user already verified))

    A["POST api/koala/account/links/register/{token}"]
    B{"Is the request valid?"}
    C[[Call service]]
    D["Check the data base for a link with provided {token} and type of register"]
    E{Link exists?}
    F{Link active?}
    G[Get the user connected with that link]
    H{Is the user verified?}
    I[Fill the user data with provided information]
    J[Deactivate the link]
    K[Return user data]

    A-->B
    B-->|YES|C
    B-->|NO|R2
    C-->D
    D-->E
    E-->|YES|F
    E-->|NO|R3
    F-->|YES|G
    F-->|NO|R3
    G-->H
    H-->|YES|R4
    H-->|NO|I
    I-->J
    J-->K
    K-->R1
```

---

## `POST api/koala/account/links/password-rest/{token}`

```mermaid
flowchart TD
    R1((Success))
    R2((402 Invalida data))
    R3((404 link does not exist or expired))
    R4((400? user not verified))

    A["POST api/koala/account/links/password-rest/{token}"]
    B{"Is the request valid?"}
    C[[Call service]]
    D["Check the data base for a link with provided {token} and type of password-reset"]
    E{Link exists?}
    F{Link active?}
    G[Get the user connected with that link]
    H{Is the user verified?}
    I[Change user password]
    J[Deactivate the link]
    K[Return user data]

    A-->B
    B-->|YES|C
    B-->|NO|R2
    C-->D
    D-->E
    E-->|YES|F
    E-->|NO|R3
    F-->|YES|G
    F-->|NO|R3
    G-->H
    H-->|YES|I
    H-->|NO|R4
    I-->J
    J-->K
    K-->R1
```

---

## `GET api/koala/account/links`

```mermaid
flowchart TD
    R1((Success))
    R2((402 Invalida data))
    R3((401 user not loged in))

    A["GET /api/koala/account/links"]
    B{"Is the request valid?"}
    C[[Call service]]
    D{"Is the user authenticated?"}
    E[Get the active links conected to the user via userID]
    F[Return the data]

    A-->B
    B-->|YES|C
    B-->|NO|R2
    C-->D
    D-->|YES|E
    D-->|NO|R3
    E-->F
    F-->R1
    
```

---

## `POST api/koala/account/links/password-reset`

```mermaid
flowchart TD
    R1((Success))
    R2((402 Invalida data))
    R3((404 User does not exist))

    A["POST api/koala/account/links/password-reset"]
    B{"Is the request valid?"}
    C[[Call service]]
    D[Get the user with provided email]
    E{"User exists?"}
    F[Generete a reset password link of type password-reset]
    G[Send the link via email]

    A-->B
    B-->|YES|C
    B-->|NO|R2
    C-->D
    D-->E
    E-->|YES|F
    E-->|NO|R3
    F-->G
    G-->R1
```

---

## `DELETE api/koala/account/links/{id}`

```mermaid
flowchart TD
    R1((Success))
    R2((402 Invalida data))
    R3((401 user not loged in))
    R4((404 link does not exist))

    A["DELETE /api/koala/account/links{id}"]
    B{"Is the request valid?"}
    C[[Call service]]
    D{"Is the user authenticated?"}
    E["Get the active link with provided {id} and conected to the user via userID"]
    F{Link exists?}
    G[Deactivate link]

    A-->B
    B-->|YES|C
    B-->|NO|R2
    C-->D
    D-->|YES|E
    D-->|NO|R3
    E-->F
    F-->|YES|G
    F-->|NO|R4
    G-->R1
```

---

## `DELETE api/koala/account/links`

```mermaid
flowchart TD
    R1((Success))
    R2((402 Invalida data))
    R3((401 user not loged in))
    R4((404 link does not exist))

    A["DELETE /api/koala/account/links"]
    B{"Is the request valid?"}
    C[[Call service]]
    D{"Is the user authenticated?"}
    E["Get the active links conected to the user via userID"]
    F[For each link if exist deactivate it]

    A-->B
    B-->|YES|C
    B-->|NO|R2
    C-->D
    D-->|YES|E
    D-->|NO|R3
    E-->F
    F-->R1
```

---