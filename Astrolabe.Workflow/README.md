# Astrolabe.Workflow

This library provides abstractions for implementing various tasks which fall under the umbrella of "workflow":

* Declarative rule based triggering of actions - e.g. automated sending emails
* Declarative security for user triggered actions - e.g. provide lists of allowed user triggerable actions and secure them if triggered by the user.
* Encourages efficient bulk operations

## The workflow executor

A workflow executor instance is responsible for:

* Loading data (possibly in bulk)
* Check rule based triggers and queue up actions
* Apply the actions which have been user triggered or automatically queued.

The class `AbstractWorkflowExecutor<TContext, TLoadContext, TAction>` provides a good base for implementing a 
workflow executor given the 3 type parameters with the following meanings. 

### TContext

This type needs to carry all the data required for editing a single entity along with any associated entities, 
e.g. Audit Logs. 

### TAction

A class which describes all the actions which can be performed, including their parameters if required.

### TLoadContext

This class should contain the data required to do a bulk load of data into `TContext` instances. 
Usually this is at least a list of id's for the entities to load.

