-- Table: public."DistCache"

-- DROP TABLE public."DistCache";

CREATE TABLE IF NOT EXISTS [schemaName].[tableName]
(
    "Id" text COLLATE pg_catalog."default" NOT NULL,
    "Value" jsonb,
    "ExpiresAtTime" timestamp with time zone,
    "SlidingExpirationInSeconds" double precision,
    "AbsoluteExpiration" timestamp with time zone,
    CONSTRAINT "DistCache_pkey" PRIMARY KEY ("Id")
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;
