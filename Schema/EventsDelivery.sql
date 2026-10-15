CREATE TABLE IF NOT EXISTS DeliveryEvents (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    ProviderEventId TEXT,              -- id from the provider, for de-duplication
    MessageId   TEXT NOT NULL,         -- matches Outbox.MessageId
    EventType   TEXT NOT NULL,         -- delivered, soft_bounce, hard_bounce, ...
    Detail      TEXT,
    ReceivedUtc TEXT NOT NULL,
    Processed   INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS IX_DeliveryEvents_Processed ON DeliveryEvents (Processed, Id);

CREATE TABLE IF NOT EXISTS DeliveryEvents_at (
    at_id           INTEGER PRIMARY KEY AUTOINCREMENT,
    transmode       TEXT NOT NULL,
    Id              INTEGER DEFAULT 0,
    ProviderEventId TEXT,
    MessageId       TEXT NOT NULL,
    EventType       TEXT NOT NULL,
    Detail          TEXT,
    ReceivedUtc     TEXT NOT NULL,
    Processed       INTEGER NOT NULL DEFAULT 0,
	device_name TEXT,
	user_name TEXT,
    at_date         TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);
CREATE INDEX IF NOT EXISTS IX_DeliveryEvents_at ON DeliveryEvents_at (Id, at_id);

CREATE TRIGGER IF NOT EXISTS DeliveryEvents_added
AFTER INSERT ON DeliveryEvents
FOR EACH ROW
BEGIN
    INSERT INTO DeliveryEvents_at
        (transmode, Id, ProviderEventId, MessageId, EventType, Detail, ReceivedUtc, Processed,device_name,user_name)
    VALUES
        ('INSERT', NEW.Id, NEW.ProviderEventId, NEW.MessageId, NEW.EventType, NEW.Detail, NEW.ReceivedUtc, NEW.Processed
		,
		  (SELECT device_name FROM app_context WHERE id = 1),
		   (SELECT user_name FROM app_context WHERE id = 1)
		);
END;

CREATE TRIGGER IF NOT EXISTS DeliveryEvents_updated
AFTER UPDATE ON DeliveryEvents
FOR EACH ROW
BEGIN
    INSERT INTO DeliveryEvents_at
        (transmode, Id, ProviderEventId, MessageId, EventType, Detail, ReceivedUtc, Processed,device_name,user_name)
    VALUES
        ('UPDATE', NEW.Id, NEW.ProviderEventId, NEW.MessageId, NEW.EventType, NEW.Detail, NEW.ReceivedUtc, NEW.Processed,
		  (SELECT device_name FROM app_context WHERE id = 1),
		   (SELECT user_name FROM app_context WHERE id = 1));
END;

CREATE TRIGGER IF NOT EXISTS DeliveryEvents_deleted
AFTER DELETE ON DeliveryEvents
FOR EACH ROW
BEGIN
    INSERT INTO DeliveryEvents_at
        (transmode, Id, ProviderEventId, MessageId, EventType, Detail, ReceivedUtc, Processed,device_name,user_name)
    VALUES
        ('DELETE', OLD.Id, OLD.ProviderEventId, OLD.MessageId, OLD.EventType, OLD.Detail, OLD.ReceivedUtc, OLD.Processed,
		  (SELECT device_name FROM app_context WHERE id = 1),
		   (SELECT user_name FROM app_context WHERE id = 1));
END;