INSERT INTO employees
(
    first_name,
    last_name,
    email,
    phone
)
VALUES
(
    'Amina',
    'Hadzic',
    'amina@example.com',
    '+38761111111'
),
(
    'Haris',
    'Kovacevic',
    'haris@example.com',
    '+38762222222'
),
(
    'Lejla',
    'Mehic',
    'lejla@example.com',
    '+38763333333'
)
ON CONFLICT (email) DO NOTHING;


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
)
ON CONFLICT (name) DO NOTHING;



INSERT INTO employee_services
(
    employee_id,
    service_id
)
SELECT
    e.id,
    s.id
FROM employees e
CROSS JOIN services s
WHERE e.email = 'haris@example.com'
  AND s.name = 'Mens Haircut'
ON CONFLICT DO NOTHING;



INSERT INTO employee_services
(
    employee_id,
    service_id
)
SELECT
    e.id,
    s.id
FROM employees e
CROSS JOIN services s
WHERE e.email = 'amina@example.com'
  AND s.name IN
  (
      'Womens Haircut',
      'Hair Coloring',
      'Hair Styling'
  )
ON CONFLICT DO NOTHING;



INSERT INTO employee_services
(
    employee_id,
    service_id
)
SELECT
    e.id,
    s.id
FROM employees e
CROSS JOIN services s
WHERE e.email = 'lejla@example.com'
  AND s.name IN
  (
      'Womens Haircut',
      'Hair Styling'
  )
ON CONFLICT DO NOTHING;



INSERT INTO appointments
(
    employee_id,
    service_id,

    customer_first_name,
    customer_last_name,
    customer_email,
    customer_phone,

    starts_at,
    ends_at,

    status,
    notes
)
SELECT
    e.id,
    s.id,

    'John',
    'Doe',
    'john.doe@example.com',
    '+38764444444',

    date_trunc('day', NOW()) + INTERVAL '2 days 10 hours',
    date_trunc('day', NOW()) + INTERVAL '2 days 10 hours 30 minutes',

    'scheduled',
    'Initial demonstration appointment.'
FROM employees e
CROSS JOIN services s
WHERE e.email = 'haris@example.com'
  AND s.name = 'Mens Haircut'
  AND NOT EXISTS
  (
      SELECT 1
      FROM appointments a
      WHERE a.customer_email = 'john.doe@example.com'
        AND a.notes = 'Initial demonstration appointment.'
  );