-- Run after 003_admin_accounts.sql. All fixtures are rolled back.
begin;
select set_config('hotel_test.admin1',gen_random_uuid()::text,true);
select set_config('hotel_test.admin2',gen_random_uuid()::text,true);
select set_config('hotel_test.staff',gen_random_uuid()::text,true);
insert into auth.users(id,email) values
    (current_setting('hotel_test.admin1')::uuid,'admin1-'||gen_random_uuid()||'@example.com'),
    (current_setting('hotel_test.admin2')::uuid,'admin2-'||gen_random_uuid()||'@example.com'),
    (current_setting('hotel_test.staff')::uuid,'staff-'||gen_random_uuid()||'@example.com');
insert into public.profiles(user_id,email,display_name,role,is_active)
select id,email,'Admin one','admin',true from auth.users where id=current_setting('hotel_test.admin1')::uuid;
insert into public.profiles(user_id,email,display_name,role,is_active)
select id,email,'Admin two','admin',true from auth.users where id=current_setting('hotel_test.admin2')::uuid;
insert into public.profiles(user_id,email,display_name,role,is_active)
select id,email,'Receptionist','receptionist',true from auth.users where id=current_setting('hotel_test.staff')::uuid;

set local role authenticated;
select set_config('request.jwt.claim.sub',current_setting('hotel_test.admin1'),true);
do $$
begin
    if not private.is_admin() or not private.is_staff() then raise exception 'FAIL: first admin has no access'; end if;
    if not exists(select 1 from public.profiles where user_id=current_setting('hotel_test.admin2')::uuid) then
        raise exception 'FAIL: first admin cannot list secondary admin'; end if;
end;
$$;
select set_config('request.jwt.claim.sub',current_setting('hotel_test.admin2'),true);
do $$
begin
    if not private.is_admin() or not private.is_staff() then raise exception 'FAIL: second admin lacks equal access'; end if;
    if not exists(select 1 from public.profiles where user_id=current_setting('hotel_test.admin1')::uuid) then
        raise exception 'FAIL: secondary admin cannot manage account listing'; end if;
    begin
        update public.profiles set role='admin' where user_id=current_setting('hotel_test.staff')::uuid;
        raise exception 'FAIL: client can change roles directly';
    exception when insufficient_privilege then null;
    end;
end;
$$;
select set_config('request.jwt.claim.sub',current_setting('hotel_test.staff'),true);
do $$
begin
    if private.is_admin() then raise exception 'FAIL: receptionist gained admin rights'; end if;
    if exists(select 1 from public.profiles where role='admin') then raise exception 'FAIL: receptionist can list admin accounts'; end if;
end;
$$;
reset role;
update public.profiles set is_active=false where user_id=current_setting('hotel_test.admin2')::uuid;
set local role authenticated;
select set_config('request.jwt.claim.sub',current_setting('hotel_test.admin2'),true);
do $$
begin
    if private.is_admin() then raise exception 'FAIL: disabled admin retained access'; end if;
    if exists(select 1 from public.profiles where user_id=current_setting('hotel_test.admin1')::uuid) then
        raise exception 'FAIL: disabled admin can list accounts'; end if;
end;
$$;
reset role;
rollback;
select 'PASS: equal admin access, receptionist isolation, disabled accounts and protected role writes' as result;
