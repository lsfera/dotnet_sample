CREATE KEYSPACE IF NOT EXISTS fulfillment
    WITH REPLICATION = { 'class': 'SimpleStrategy', 'replication_factor': 1 };

CREATE TABLE IF NOT EXISTS fulfillment.orders (
    id text,
    country text,
    created_at timestamp,
    amount decimal,
    currency text,
    status text,
    PRIMARY KEY ((id), country)
);
