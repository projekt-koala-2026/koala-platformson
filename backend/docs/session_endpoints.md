# Session Endpoints

---
## `POST api/koala/account/sessions`

```mermaid
flowchart TD
    R1((Success))
    R2((402 Invalida data))
    R3((400? User already logedin))
    R4((404 Invalid email or password))

    A["POST api/koala/account/sessions"]
    B{"Is the request valid?"}
    C[[Call service]]
    D{Is user Authenticated}
    E[Get the user with provided email]
    F{User exists?}
    G{Does user password_hash and provided passward after hasing match}
    H[Created session for user]
    I[Create a secure session cookie storing session token]
    J[Return session and user data without the session token]

    A-->B
    B-->|YES|C
    B-->|NO|R2
    C-->D
    D-->|YES|R3
    D-->|NO|E
    E-->F
    F-->|YES|G
    F-->|NO|R4
    G-->|YES|H
    G-->|NO|R4
    H-->I
    I-->J
    J-->R1
```
---
## `DELETE api/koala/account/sessions`

```mermaid
flowchart TD
    R1((Success))
    R2((402 Invalida data))
    R3((401 User not logedin))
    R4((404 Session does not exist))

    A["DELETE api/koala/account/sessions"]
    B{"Is the request valid?"}
    C[[Call service]]
    D{Is user Authenticated}
    E[Get the session with cookie session_token]
    F{Session exists?}
    G[Deactivate session]

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
## `GET api/koala/account/sessions`

```mermaid
flowchart TD
    R1((Success))
    R2((402 Invalida data))
    R3((401 User not logedin))

    A["GET api/koala/account/sessions"]
    B{"Is the request valid?"}
    C[[Call service]]
    D{Is user Authenticated}
    E[Get the sessions conected to a user via user Id in claims]
    F[Return data]

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
## `DELETE api/koala/account/sessions/{id}`

```mermaid
flowchart TD
    R1((Success))
    R2((402 Invalida data))
    R3((401 User not logedin))
    R4((404 Session does not exist))

    A["GET api/koala/account/sessions"]
    B{"Is the request valid?"}
    C[[Call service]]
    D{Is user Authenticated}
    E[Get the session with provided id and conected to a user via user ID claims]
    F{Session exist?}
    G[Delete session]

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

## `DELETE api/koala/account/sessions/all`

```mermaid
flowchart TD
    R1((Success))
    R2((402 Invalida data))
    R3((401 User not logedin))

    A["GET api/koala/account/sessions"]
    B{"Is the request valid?"}
    C[[Call service]]
    D{Is user Authenticated}
    E[Get the sessions conected to a user via user ID claims]
    F[For each session if exists delete it]
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