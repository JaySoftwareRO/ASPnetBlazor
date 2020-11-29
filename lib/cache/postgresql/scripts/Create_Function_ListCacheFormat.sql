-- FUNCTION: public.listcacheitemformat(text, text, timestamp with time zone)
-- DROP FUNCTION public.listcacheitemformat(text, text, timestamp with time zone);

CREATE OR REPLACE FUNCTION [schemaName].listcacheitemformat(
	"SchemaName" text,
	"TableName" text,
	"UtcNow" timestamp with time zone
    )
    RETURNS TABLE(distcache_id text)
    LANGUAGE 'plpgsql'
    COST 100.0
    VOLATILE NOT LEAKPROOF 
    ROWS 1000.0
AS $function$

    DECLARE v_Query Text;
    DECLARE	var_r record;
        
    BEGIN
        v_Query := format('SELECT "Id" FROM %I.%I WHERE $1 <= "ExpiresAtTime"', "SchemaName", "TableName");

        FOR var_r IN EXECUTE v_Query USING "UtcNow"
        LOOP
            DistCache_Id := var_r."Id" ; 
            RETURN NEXT;
        END LOOP;
    END

$function$;

