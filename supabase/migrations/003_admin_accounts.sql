-- Applies to both a new 001 schema and an existing 002 deployment.
-- Existing highest administrators become ordinary admins; passwords are unchanged.
begin;

drop policy if exists super_admin_read_profiles on public.profiles;
drop function if exists private.is_super_admin();
drop index if exists public.one_super_admin;

update public.profiles set role = 'admin',
    display_name = case when display_name = 'Quản trị cao nhất' then 'Quản trị viên' else display_name end
where role = 'super_admin';

alter table public.profiles drop constraint if exists profiles_role_check;
alter table public.profiles add constraint profiles_role_check check (role in ('admin','receptionist'));

create or replace function private.is_staff() returns boolean
language sql stable security definer set search_path = '' as $$
    select exists(select 1 from public.profiles where user_id = (select auth.uid())
        and is_active and role in ('admin','receptionist'));
$$;

create or replace function private.is_admin() returns boolean
language sql stable security definer set search_path = '' as $$
    select exists(select 1 from public.profiles where user_id = (select auth.uid())
        and is_active and role = 'admin');
$$;

drop policy if exists admin_read_profiles on public.profiles;
create policy admin_read_profiles on public.profiles for select to authenticated
    using ((select private.is_admin()));

alter table public.profiles add column if not exists creation_request_id uuid unique;
grant select, insert on public.profiles to service_role;
commit;

select role, count(*) as accounts from public.profiles group by role order by role;
