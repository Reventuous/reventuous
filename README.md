# Reventuous

Event Sourcing library for C# using Redis Streams as event store.
This library is based on the Eventuous library which targets EventStoreDB.

The main driver behind this project is to use one database type for both the write site (event store) and read side (projections).

Redis fits this bill as:
- it provides the streams type that can be used as event store and for subscriptions. 
- it is mature enough to be used as the main database
- provides replication and persistency (on disk)
- it is very fast since all Redis data resides in memory (perfect for the read side)
- it can be scaled horizontally using sharding (cluster mode)
- it can be extended with modules (Redis Search for full text search, Redis Graph to turn Redis into a graph database, Redis Gears for running functions on the server side which can be used to create projections on the fly, and many more)
- for a full list of Redis modules: https://redis.io/modules

Authors:
- Claudiu Chis

License: MIT

Use it at your own risk.

Where to start:
- The test folder provides tests/examples for how to use Redis as an event store, and how to create subscriptions to consume events within a C# application.

Redis Streams doesn't come with system projections like with EventStoreDB (e.g. $all, $by-category, or $by-event-type), however it's possible to get these projections by following these steps:
- install the Redis Gears module, which enables Python functions to on the Redis server 
- register this Pyhthon code as a Redis Gears function, which will create 3 projections $all, $by-category, or $by-event-type:

```
def link(x):
    # project only events for non-projected streams (e.g. not starting with $)
    if x['key'].startswith('$') is False:
        # $all
        execute('xadd', '$all', '*', 'stream', x['key'], 'position', x['id'])
        # $by-category:<category>
        category = x['key'].split(':')
        execute('xadd', '$by-category:' + category[0], '*', 'stream', x['key'], 'position', x['id'])
        # $by-event-type:<event-type>
        execute('xadd', '$by-event-type:' + x['value']['type'], '*', 'stream', x['key'], 'position', x['id'])

gb = GearsBuilder('StreamReader')
gb.foreach(link)
gb.register(prefix='*', duration=1, batch=1, trimStream=False)

```