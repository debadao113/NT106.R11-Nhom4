-- Run in SQL Editor after 001_hotel.sql. All fixtures are rolled back.
begin;

select set_config('hotel_test.admin', gen_random_uuid()::text, true);
select set_config('hotel_test.receptionist', gen_random_uuid()::text, true);
select set_config('hotel_test.outsider', gen_random_uuid()::text, true);

insert into auth.users(id, email) values
    (current_setting('hotel_test.admin')::uuid, 'test-admin-' || gen_random_uuid() || '@example.com'),
    (current_setting('hotel_test.receptionist')::uuid, 'test-staff-' || gen_random_uuid() || '@example.com'),
    (current_setting('hotel_test.outsider')::uuid, 'test-other-' || gen_random_uuid() || '@example.com');
insert into public.profiles(user_id,email,display_name,role)
select id,email,'Test administrator','admin' from auth.users where id=current_setting('hotel_test.admin')::uuid;
insert into public.profiles(user_id,email,display_name,role)
select id,email,'Test receptionist','receptionist' from auth.users where id=current_setting('hotel_test.receptionist')::uuid;

do $$
begin
    if has_function_privilege('anon', 'public.hotel_get_rooms()', 'execute') then
        raise exception 'FAIL: anon can call staff functions';
    end if;
    if has_table_privilege('authenticated', 'public.profiles', 'insert,update,delete') then
        raise exception 'FAIL: clients can edit their roles';
    end if;
    if has_table_privilege('authenticated', 'public.stays', 'insert,update,delete') then
        raise exception 'FAIL: clients can bypass stay transactions';
    end if;
end;
$$;

set local role authenticated;
select set_config('request.jwt.claim.sub', current_setting('hotel_test.admin'), true);
insert into public.rooms(number,room_type,bed,price) values ('TEST-' || gen_random_uuid(),'Double','Double',500000);
select set_config('hotel_test.room_id', id::text, true) from public.rooms where number like 'TEST-%' order by id desc limit 1;
insert into public.employees(name,phone,gender,email) values ('Test employee','0900000000','Other','test@example.com');

select set_config('request.jwt.claim.sub', current_setting('hotel_test.receptionist'), true);
do $$
declare stay_id integer; data jsonb;
begin
    begin
        insert into public.employees(name,phone,email) values ('Denied','0900000000','denied@example.com');
        raise exception 'FAIL: receptionist can insert employees';
    exception when insufficient_privilege then null;
    end;
    if exists(select 1 from public.employees) then raise exception 'FAIL: receptionist can see employee data'; end if;

    data := jsonb_build_object('RoomId',current_setting('hotel_test.room_id')::integer,'Name','Test guest',
        'Phone','0900000000','IdentityDocument','TEST-DOCUMENT','CheckIn','2026-01-01','BirthDate','2000-01-01');
    stay_id := public.hotel_check_in(data);
    if not exists(select 1 from jsonb_array_elements(public.hotel_get_rooms()) r
        where (r->>'Id')::integer=current_setting('hotel_test.room_id')::integer and (r->>'IsOccupied')::boolean)
        then raise exception 'FAIL: occupied room is shown as available'; end if;
    begin
        perform public.hotel_check_in(data);
        raise exception 'FAIL_DUPLICATE';
    exception when raise_exception then
        if sqlerrm = 'FAIL_DUPLICATE' then raise; end if;
    end;
    begin
        perform public.hotel_check_out(stay_id,'2025-12-31');
        raise exception 'FAIL_DATE';
    exception when raise_exception then
        if sqlerrm = 'FAIL_DATE' then raise; end if;
    end;
    perform public.hotel_check_out(stay_id,'2026-01-02');
    if exists(select 1 from jsonb_array_elements(public.hotel_get_rooms()) r
        where (r->>'Id')::integer=current_setting('hotel_test.room_id')::integer and (r->>'IsOccupied')::boolean)
        then raise exception 'FAIL: checkout did not release the room'; end if;
end;
$$;

select set_config('request.jwt.claim.sub', current_setting('hotel_test.outsider'), true);
do $$
begin
    begin
        perform public.hotel_get_rooms();
        raise exception 'FAIL: user without profile can read rooms';
    exception when insufficient_privilege then null;
    end;
    begin
        perform public.hotel_check_out(1,current_date);
        raise exception 'FAIL: user without profile can check out guests';
    exception when insufficient_privilege then null;
    end;
end;
$$;
reset role;
update public.profiles set is_active=false where user_id=current_setting('hotel_test.receptionist')::uuid;
set local role authenticated;
select set_config('request.jwt.claim.sub', current_setting('hotel_test.receptionist'), true);
do $$
begin
    begin
        perform public.hotel_get_rooms();
        raise exception 'FAIL: inactive user can still read rooms';
    exception when insufficient_privilege then null;
    end;
end;
$$;
reset role;
rollback;
select 'PASS: permissions, check-in conflict, dates, checkout and inactive users' as result;
