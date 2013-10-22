NGinn.BPM
=========

NGinn.BPM is a workflow engine for Microsoft.Net. It employs a process modeling language similar to BPMN in key concepts and a Petri-net based process execution engine.

The main design objectives of this project are:

*   Ligtweight

    Easy to embed in any kind of application, without too many external dependencies, bloat controlled tightly

*   Transactional, bulletproof

    ACID properties for long running processes (data durability, atomic updates, consistency in case of failures, general reliability and robustness)

*   Expressive like BPMN

    One of goals here is full BPMN support, but we concentrate on process definitions being concise, human-readable and human-authorable (without using code generators)

*   Powerful and extensible
    
    So it can take care of many aspects of application logic, not only the process flow. 

*   Performant

    We pay great attention to I/O and memory efficiency of the engine
    
Key features
------------
*   Operation modes

    NGinn.BPM can be embedded in applications or can run as a standalone service. Processes can be run in persistent mode (intermediate state stored in database) or in memory only. 
    Synchronous and asynchronous execution is supported (to the extent allowed by process model).
    
*   Process model elements
    
    Almost all constructs known in BPMN can be modeled in NGinn.BPM

*   Data model
    
    The data model is an element of process definition. Data structures, validation rules and binding rules are defined in the model and enforced during process
    execution.
    
*   SQL Server backend
    
    By default NGinn.BPM uses SQL Server backend, but it can be easily ported to other SQL databases. 
    