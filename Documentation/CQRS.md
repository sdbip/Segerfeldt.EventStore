<!--
    This comment only exists to disable the Markdownlint rule
    MD025/single-title/single-h1: Multiple top-level headings in the same document
    This behaviour was observed when using https://marketplace.visualstudio.com/items?itemName=DavidAnson.vscode-markdownlint
-->

# Command/Query Responsibility Segregation

(Not to be confused with Command/Query Separation, [CQS](#is-cqrs-similar-to-cqs).)

# What is CQRS?

The idea of CQRS is to maintain two different models of the application state, a *read* model and a *write* model. These two models do not share code, and they might even not share data. The write-model data is however the “truth” and the read-model is expected to be a mirror of that state.

The point of having two models are many. Here are a few:

- The read-model should focus on querying the state of the application. It should use a flexible model that allows innovative querying.
- The read-model should ideally have a data structure that matches its most common queries.
- The write-model maintains the invariants and business rules of the domain. It needs a storage structure that aids in maintaining consistency.
- The write-model might use event sourcing.
- With two separate models, each can be optimised for its specific needs.
- Storage is not very expensive.
- It is easier to program when there is less coupling.

The Command side (a.k.a. the write-model) is the complex side. It needs to ensure that the state of the system remains consistent, that no invariants are violated and that the domain rules are followed. The query side (read-model), however is simple. By the time the read-model has the data it will have to have been accepted by the write-model's validations.

# Eventual Consistency

When synchronising the state of two separate databases, there is no way to guarantee that both are always exact replicas of each other. In CQRS however, only one side makes actual changes and synchronisation need only go in one direction.

When the command side makes an alteration, that change is not immediately visible on the query side. Expect that it takes a second (or sometimes longer) to propagate the change to the read-model. The read-model will *eventually* be consistent however. This phenomenon is therefore called *eventual consistency*. It has that name even if the command side were to make changes so often that the read-model can never catch up, and never be fully synchronised.

In some systems eventual consistency goes fast; in some slow. Some systems do not guarantee (or even expect) consistency until the next day. EventStore however should typically be consistent within minutes.

# Is CQRS Similar to CQS?

No! CQRS is not CQS, and there is very little to connect the two other than the similarity of their acronyms. The point of CQS is to disallow functions that return a value while also modifying state. CQRS has no limitation as to where queries and state manipulations can occur.

The point of CQRS is not to segregate reads from writes; it is to maintain two different *models* of the state. One that facilitates easy and flexible reads of the state data, and another that maintains the invariants and business rules of the domain model.

There is however no rule that prevents reading the state of the write-model; the event history of any entity is clearly valuable information to clients. Writing to the read-model on the other hand should probably be prohibited (other than to keep it in sync with the write-model).
