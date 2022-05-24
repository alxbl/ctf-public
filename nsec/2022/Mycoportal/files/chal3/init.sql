PRAGMA foreign_keys = ON;

DROP TABLE IF EXISTS ApiKeys;
DROP TABLE IF EXISTS Users;

CREATE TABLE Users (
   id        INT PRIMARY KEY NOT NULL,
   name      TEXT UNIQUE NOT NULL
);

CREATE TABLE ApiKeys (
 id          INTEGER PRIMARY KEY AUTOINCREMENT,
 uid         INT NOT NULL REFERENCES Users(id),
 token       TEXT NOT NULL
);


INSERT INTO Users (id, name) VALUES (1, 'admin');
INSERT INTO Users (id, name) VALUES (2, 'guest');

INSERT INTO ApiKeys (id, uid, token) VALUES
(1, 1, 'FLAG-d9cf1c1ac494062e44d60902769441070113dca8'), -- admin: Mycoverse Portal (3/4)
(2, 2, 'FLAG-152257cc0c1c123b0f0a35a706bb9c81c0d94a7f'); -- guest: Mycoverse Portal (Hint 3)
