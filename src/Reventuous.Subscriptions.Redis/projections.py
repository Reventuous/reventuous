#
# This Python script needs to be added to Redis as a RedisGears function.
# It creates system projections like in EventStoreDB:
# 
# 1. All events ($all) - a stream which links to all events from all the streams
# 1. By Category ($by-category) - a stream which links to all events for the same category/aggregate type (e.g. account)
# 2. By Event Type ($by-event-type) - a stream with all events of the same type from different streams, e.g. AccountCreated.
# 
# These streams don't include the original events only a link to those events.
# 

#
# link(x)
# For each event it creates the 3 different projections ($all, $by-category, $by-event-type)
# 
# An event is supplied in this format:
#   ["{'key': 'account:208add97-4d22-4dc2-a986-cd1600182a5b', 'id': '1634409908001-0', 'value': {'type': 'AccountCreated'}}"]
# where:
#   key - is the stream id where the event exists
#   id - is the id of the event within the stream
#   value - is the event payload
# 
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
