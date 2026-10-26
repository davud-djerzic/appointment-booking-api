BEGIN;

INSERT INTO user_accounts
    (first_name, last_name, email, password_hash, role)
VALUES
    ('Mehmed', 'Efendić', 'mehmed.efendic@gmail.com',
     'AQAAAAIAAYagAAAAEAoN91M2gW+0dwgorLTQrUutmm/kzpFhm6fhM2mPwKQbDhiEriVtw3rK1Ia7dl22XQ==',
     'admin')
returning id;

INSERT INTO employees (user_account_id, phone)
VALUES (1, '+387 60 32 57 129');

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
VALUES
    (1, 1, '08:00:00', '17:00:00'),
    (1, 2, '08:00:00', '17:00:00'),
    (1, 3, '08:00:00', '17:00:00'),
    (1, 4, '08:00:00', '17:00:00'),
    (1, 5, '08:00:00', '17:00:00');

INSERT INTO employee_working_hours
    (employee_id, day_of_week, starts_at, ends_at)
VALUES
    (1, 1, '08:00:00', '12:00:00'),
    (1, 1, '13:00:00', '17:00:00'),
    (1, 2, '08:00:00', '12:00:00'),
    (1, 2, '13:00:00', '17:00:00'),
    (1, 3, '08:00:00', '12:00:00'),
    (1, 3, '13:00:00', '17:00:00'),
    (1, 4, '08:00:00', '12:00:00'),
    (1, 4, '13:00:00', '17:00:00'),
    (1, 5, '08:00:00', '12:00:00'),
    (1, 5, '13:00:00', '17:00:00');

insert into services(name, duration_minutes, price) 
values('Šišanje', 30, 13);

insert into services(name, duration_minutes, price) 
values('Brijanje', 7, 15);

insert into services(name, duration_minutes, price) 
values('Oblikovanje brade', 7, 15);

insert into services(name, duration_minutes, price) 
values('Pranje kose', 5, 5);

insert into employee_services(employee_id, service_id) 
values(1, 1);

insert into employee_services(employee_id, service_id) 
values(1, 2);

insert into employee_services(employee_id, service_id) 
values(1, 3);

insert into employee_services(employee_id, service_id) 
values(1, 4);

COMMIT;
