-- Initial data for a NEW, empty database. Run once, after 001–013 and before 015.
-- Rows are linked by natural keys (email, salon name), not by hard-coded IDs,
-- so the script stays correct even if an IDENTITY value was already consumed.

-- The file is UTF-8 (Šišanje, Efendić, šiša); this keeps the text correct
-- whatever the client's default encoding is (e.g. psql on Windows).
SET client_encoding = 'UTF8';

BEGIN;

INSERT INTO user_accounts
    (first_name, last_name, email, password_hash, role)
VALUES
    ('Mehmed', 'Efendić', 'mehmed.efendic@gmail.com',
     'AQAAAAIAAYagAAAAEAoN91M2gW+0dwgorLTQrUutmm/kzpFhm6fhM2mPwKQbDhiEriVtw3rK1Ia7dl22XQ==',
     'admin');

INSERT INTO employees (user_account_id, phone)
SELECT ua.id, '+387 60 32 57 129'
FROM user_accounts AS ua
WHERE ua.email = 'mehmed.efendic@gmail.com';

INSERT INTO salons (name, address, city, instagram_url, phone)
VALUES (
    'Ko te šiša',
    'Tušanj 13',
    'Tuzla',
    'https://www.instagram.com/kotesisa.tuzla/',
    '+387 60 32 57 129'
);

INSERT INTO salon_working_hours
    (salon_id, day_of_week, starts_at, ends_at)
SELECT s.id, v.day_of_week, v.starts_at, v.ends_at
FROM salons AS s
CROSS JOIN (
    VALUES
        (1, TIME '08:00', TIME '17:00'),
        (2, TIME '08:00', TIME '17:00'),
        (3, TIME '08:00', TIME '17:00'),
        (4, TIME '08:00', TIME '17:00'),
        (5, TIME '08:00', TIME '17:00')
) AS v (day_of_week, starts_at, ends_at)
WHERE s.name = 'Ko te šiša';

INSERT INTO employee_working_hours
    (employee_id, day_of_week, starts_at, ends_at)
SELECT e.id, v.day_of_week, v.starts_at, v.ends_at
FROM employees AS e
INNER JOIN user_accounts AS ua
    ON ua.id = e.user_account_id
CROSS JOIN (
    VALUES
        (1, TIME '08:00', TIME '12:00'),
        (1, TIME '13:00', TIME '17:00'),
        (2, TIME '08:00', TIME '12:00'),
        (2, TIME '13:00', TIME '17:00'),
        (3, TIME '08:00', TIME '12:00'),
        (3, TIME '13:00', TIME '17:00'),
        (4, TIME '08:00', TIME '12:00'),
        (4, TIME '13:00', TIME '17:00'),
        (5, TIME '08:00', TIME '12:00'),
        (5, TIME '13:00', TIME '17:00')
) AS v (day_of_week, starts_at, ends_at)
WHERE ua.email = 'mehmed.efendic@gmail.com';

INSERT INTO services (name, duration_minutes, price)
VALUES
    ('Šišanje', 30, 13),
    ('Brijanje', 15, 7),
    ('Oblikovanje brade', 15, 7),
    ('Pranje kose', 5, 5);

-- Every active service is provided by every active employee.
INSERT INTO employee_services (employee_id, service_id)
SELECT e.id, s.id
FROM employees AS e
INNER JOIN user_accounts AS ua
    ON ua.id = e.user_account_id
CROSS JOIN services AS s
WHERE ua.email = 'mehmed.efendic@gmail.com'
  AND s.is_active = TRUE;

COMMIT;
