INSERT INTO user_accounts
(
    email,
    password_hash,
    role
)
VALUES
(
    'amina@example.com',
    'AQAAAAIAAYagAAAAEHXIUjgpyiideBy7kCYj6f3WrUgesm9RZMarAbeTKwTnNYmkwkvFhbhOYyX7lExnPw==',
    'employee'
),
(
    'haris@example.com',
    'AQAAAAIAAYagAAAAEHXIUjgpyiideBy7kCYj6f3WrUgesm9RZMarAbeTKwTnNYmkwkvFhbhOYyX7lExnPw==',
    'employee'
),
(
    'lejla@example.com',
    'AQAAAAIAAYagAAAAEHXIUjgpyiideBy7kCYj6f3WrUgesm9RZMarAbeTKwTnNYmkwkvFhbhOYyX7lExnPw==',
    'employee'
),
(
    'admin@example.com',
    'AQAAAAIAAYagAAAAEHXIUjgpyiideBy7kCYj6f3WrUgesm9RZMarAbeTKwTnNYmkwkvFhbhOYyX7lExnPw==',
    'admin'
),
(
    'john.doe@example.com',
    'AQAAAAIAAYagAAAAEHXIUjgpyiideBy7kCYj6f3WrUgesm9RZMarAbeTKwTnNYmkwkvFhbhOYyX7lExnPw==',
    'customer'
);


INSERT INTO employees
(
    user_account_id,
    first_name,
    last_name,
    phone
)
SELECT
    id,
    'Amina',
    'Hadzic',
    '+38761111111'
FROM user_accounts
WHERE email = 'amina@example.com';


INSERT INTO employees
(
    user_account_id,
    first_name,
    last_name,
    phone
)
SELECT
    id,
    'Haris',
    'Kovacevic',
    '+38762222222'
FROM user_accounts
WHERE email = 'haris@example.com';


INSERT INTO employees
(
    user_account_id,
    first_name,
    last_name,
    phone
)
SELECT
    id,
    'Lejla',
    'Mehic',
    '+38763333333'
FROM user_accounts
WHERE email = 'lejla@example.com';


INSERT INTO customers
(
    user_account_id,
    first_name,
    last_name,
    phone
)
SELECT
    id,
    'John',
    'Doe',
    '+38764444444'
FROM user_accounts
WHERE email = 'john.doe@example.com';


INSERT INTO services
(
    name,
    description,
    duration_minutes,
    price
)
VALUES
(
    'Mens Haircut',
    'Standard haircut service for men.',
    30,
    15.00
),
(
    'Womens Haircut',
    'Standard haircut service for women.',
    45,
    25.00
),
(
    'Hair Coloring',
    'Professional hair coloring service.',
    90,
    60.00
),
(
    'Hair Styling',
    'Professional hair styling service.',
    45,
    30.00
);


INSERT INTO employee_services
(
    employee_id,
    service_id
)
SELECT
    e.id,
    s.id
FROM employees e
JOIN user_accounts ua
    ON ua.id = e.user_account_id
CROSS JOIN services s
WHERE ua.email = 'haris@example.com'
  AND s.name = 'Mens Haircut';


INSERT INTO employee_services
(
    employee_id,
    service_id
)
SELECT
    e.id,
    s.id
FROM employees e
JOIN user_accounts ua
    ON ua.id = e.user_account_id
CROSS JOIN services s
WHERE ua.email = 'amina@example.com'
  AND s.name IN
  (
      'Womens Haircut',
      'Hair Coloring',
      'Hair Styling'
  );


INSERT INTO employee_services
(
    employee_id,
    service_id
)
SELECT
    e.id,
    s.id
FROM employees e
JOIN user_accounts ua
    ON ua.id = e.user_account_id
CROSS JOIN services s
WHERE ua.email = 'lejla@example.com'
  AND s.name IN
  (
      'Womens Haircut',
      'Hair Styling'
  );


INSERT INTO employee_working_hours
(
    employee_id,
    day_of_week,
    starts_at,
    ends_at
)
SELECT
    e.id,
    day_value,
    TIME '09:00',
    TIME '17:00'
FROM employees e
JOIN user_accounts ua
    ON ua.id = e.user_account_id
CROSS JOIN
(
    VALUES
        (1),
        (2),
        (3),
        (4),
        (5)
) AS days(day_value)
WHERE ua.email = 'amina@example.com';


INSERT INTO employee_working_hours
(
    employee_id,
    day_of_week,
    starts_at,
    ends_at
)
SELECT
    e.id,
    day_value,
    TIME '10:00',
    TIME '18:00'
FROM employees e
JOIN user_accounts ua
    ON ua.id = e.user_account_id
CROSS JOIN
(
    VALUES
        (1),
        (2),
        (3),
        (4),
        (5)
) AS days(day_value)
WHERE ua.email = 'haris@example.com';


INSERT INTO employee_working_hours
(
    employee_id,
    day_of_week,
    starts_at,
    ends_at
)
SELECT
    e.id,
    day_value,
    TIME '09:00',
    TIME '15:00'
FROM employees e
JOIN user_accounts ua
    ON ua.id = e.user_account_id
CROSS JOIN
(
    VALUES
        (1),
        (2),
        (3),
        (4),
        (5)
) AS days(day_value)
WHERE ua.email = 'lejla@example.com';


INSERT INTO bookings
(
    customer_id,
    employee_id,
    starts_at,
    ends_at,
    status,
    notes
)
SELECT
    c.id,
    e.id,

    date_trunc('day', NOW()) + INTERVAL '2 days 10 hours',
    date_trunc('day', NOW()) + INTERVAL '2 days 10 hours 30 minutes',

    'scheduled',
    'Initial demonstration booking.'
FROM customers c
JOIN user_accounts cua
    ON cua.id = c.user_account_id
JOIN employees e
    ON e.user_account_id =
    (
        SELECT id
        FROM user_accounts
        WHERE email = 'haris@example.com'
    )
JOIN user_accounts eua
    ON eua.id = e.user_account_id
WHERE cua.email = 'john.doe@example.com';


INSERT INTO appointments
(
    booking_id,
    service_id,
    starts_at,
    ends_at,
    status
)
SELECT
    b.id,
    s.id,
    b.starts_at,
    b.ends_at,
    'scheduled'
FROM bookings b
JOIN customers c
    ON c.id = b.customer_id
JOIN user_accounts cua
    ON cua.id = c.user_account_id
JOIN services s
    ON s.name = 'Mens Haircut'
WHERE cua.email = 'john.doe@example.com'
  AND b.notes = 'Initial demonstration booking.';