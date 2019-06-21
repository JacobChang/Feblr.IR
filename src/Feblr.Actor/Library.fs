namespace Feblr.Actor

module Actor =
    open System.Threading.Tasks
    open FSharp.Control.Tasks

    type Request<'i, 'o> =
        | Msg of 'i
        | MsgNeedReply of 'i * AsyncReplyChannel<'o>
        | Stop

    type Response<'e> =
        | Normal
        | Abnormal of 'e
        | Exit of 'e

    type ActorRef<'i, 'o> =
        { mailbox: MailboxProcessor<Request<'i, 'o>> }

        member this.Post (msg: 'i) =
            let msg: Request<'i, 'o> = Msg msg
            this.mailbox.Post msg

    let (<!) (actorRef: ActorRef<'i, 'o>) (msg: 'i) =
        actorRef.Post msg

    let (<?) (actorRef: ActorRef<'i, 'o>) (msg: 'i) =
        actorRef.mailbox.PostAndReply (fun channel -> MsgNeedReply (msg, channel))

    let (<~?) (actorRef: ActorRef<'i, 'o>) (msg: 'i) =
        actorRef.mailbox.PostAndAsyncReply (fun channel -> MsgNeedReply (msg, channel))

    type Actor<'s, 'm, 'e> =
        { start: unit -> Async<'s * Response<'e>>
          behaviour: 's -> 'm -> Async<'s * Response<'e>>
          stop: 's -> unit }

        static member create start behaviour stop =
            { start = start; behaviour = behaviour; stop = stop }

        static member spawn (actor: Actor<'s, 'r, 'e>) = async {
            let! (state, response) = actor.start ()
            match response with
            | Normal ->
                let mailbox = MailboxProcessor.Start (fun inbox ->
                    let rec loop (state: 's) = async {
                        let! msg = inbox.Receive()
                        match msg with
                        | Msg msg ->
                            let! (newState, response) = actor.behaviour state msg
                            match response with
                            | Normal ->
                                return! loop newState
                            | _ ->
                                actor.stop state
                        | Stop ->
                            actor.stop state
                    }

                    loop state
                )

                return Ok { mailbox = mailbox }
            | Abnormal e ->
                return Error e
            | Exit e ->
                return Error e
        }
