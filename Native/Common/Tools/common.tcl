###############################################################################
#
# common.tcl --
#
# Extensible Adaptable Generalized Logic Engine (Eagle)
# Eagle Common Tools Package
#
# Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
#
# See the file "license.terms" for information on usage and redistribution of
# this file, and for a DISCLAIMER OF ALL WARRANTIES.
#
# RCS: @(#) $Id: $
#
###############################################################################

#
# NOTE: This script file uses features that are only present in Tcl 8.4 or
#       higher (e.g. the "eq" operator for [expr], etc).
#
if {![package vsatisfies [package provide Tcl] 8.4]} then {
  error "need Tcl 8.4 or higher"
}

#
# NOTE: This script file uses features that are not available or not needed
#       in Eagle (e.g. the "http" and "tls" packages, etc).
#
if {[catch {package present Eagle}] == 0} then {
  error "need native Tcl"
}

###############################################################################

namespace eval ::Eagle::Tools::Common {
  #
  # NOTE: *HACK* Skip defining this procedure if it is already defined in the
  #       global namespace.
  #
  if {[llength [info commands ::appendArgs]] == 0} then {
    #
    # NOTE: This procedure was stolen from the "auxiliary.eagle" script.
    #       This procedure accepts an any number of arguments.  The arguments
    #       are appended into one big string, verbatim.  The resulting string
    #       is returned.  Normally, this procedure is used to avoid undesired
    #       string interpolation operations.
    #
    # <ignore>
    proc appendArgs { args } {
      # <help>
      # This procedure concatenates all of its arguments into a single string,
      # verbatim, and returns that string.  It exists to avoid the undesired
      # string interpolation (and quoting) that can occur when building up
      # strings with [subst]-like constructs; by passing each piece as its own
      # argument, callers can compose messages, URIs, file names, and similar
      # text without worrying about embedded spaces or special characters being
      # reinterpreted.  It is one of the most heavily used helpers in this
      # package and is the native-Tcl counterpart to the Eagle command of the
      # same name.
      #
      # How it works: it uses [eval append result $args] so that the "args"
      # list is expanded and each element is appended, in order, to the
      # initially unset "result" variable.  The value of that final [append] is
      # the procedure result; there is no explicit [return].
      #
      # Tricky details: this definition is only installed when no command named
      # ::appendArgs already exists in the global namespace, so an existing
      # (for example, Eagle-provided) implementation always wins.  Because the
      # arguments are joined with no separator, the caller is responsible for
      # supplying any spaces or punctuation as additional arguments.
      #
      # Arguments:
      #   args -- Zero or more values.  Each value is appended, in order, with
      #           no separator between them.  With no arguments, the empty
      #           string is returned.
      #
      # Results:
      #   The string formed by concatenating all of the arguments.
      # </help>

      eval append result $args
    }
  }

  #
  # NOTE: *HACK* Skip defining this procedure if it is already defined in the
  #       global namespace.
  #
  if {[llength [info commands ::makeBinaryChannel]] == 0} then {
    #
    # NOTE: This procedure was stolen from the "file1.eagle" script.  This
    #       procedure reconfigures the specified channel to full binary mode.
    #
    # <ignore>
    proc makeBinaryChannel { channel } {
      # <help>
      # This procedure reconfigures the specified open channel for full binary
      # operation and returns the result of the [fconfigure] command.  It
      # exists so that downloaded package files, signature files, and other
      # non-textual data are read and written byte-for-byte, without any
      # character-set or end-of-line translation corrupting the contents.  This
      # is essential when handling content whose integrity is later verified
      # (for example, against an OpenPGP signature), since any translation
      # would alter the bytes and invalidate the verification.
      #
      # How it works: it sets both the channel encoding to "binary" and the
      # channel translation to "binary" in a single [fconfigure] call.  This
      # disables newline translation and treats the byte stream literally.
      #
      # Tricky details: this definition is only installed when no command named
      # ::makeBinaryChannel already exists in the global namespace, so an
      # existing implementation always wins.  The channel must already be open;
      # this procedure does not open or close it.
      #
      # Arguments:
      #   channel -- The identifier of an already-open channel to reconfigure
      #              for binary input and/or output.
      #
      # Results:
      #   The result of the underlying [fconfigure] call, normally the empty
      #   string.
      # </help>

      fconfigure $channel -encoding binary -translation binary; # BINARY DATA
    }
  }

  #
  # NOTE: *HACK* Skip defining this procedure if it is already defined in the
  #       global namespace.
  #
  if {[llength [info commands ::writeFile]] == 0} then {
    #
    # NOTE: This procedure was stolen from the "file1.eagle" script.  This
    #       procedure writes all data to the specified binary file and returns
    #       an empty string.  Previous data contained in the file, if any, is
    #       lost.
    #
    # <ignore>
    proc writeFile { fileName data } {
      # <help>
      # This procedure writes the specified data to the specified file as raw
      # binary content, replacing any previous contents of that file, and
      # returns the empty string.  It exists to provide a single, reliable way
      # to persist downloaded payloads (such as package archives and OpenPGP
      # key or signature files) to disk exactly as received, so that the bytes
      # on disk match the bytes that were transferred.
      #
      # How it works: it opens the file for writing using the access flags
      # WRONLY, CREAT, and TRUNC (create if absent, truncate if present),
      # reconfigures the resulting channel to binary mode via
      # [makeBinaryChannel], writes the data with [puts -nonewline] so that no
      # trailing newline is added, and then closes the channel.
      #
      # Tricky details: this definition is only installed when no command named
      # ::writeFile already exists in the global namespace, so an existing
      # implementation always wins.  Any prior contents of the target file are
      # unconditionally lost because of the TRUNC flag.  Because the write is
      # not wrapped in a [catch], an error opening, writing, or closing the
      # file propagates to the caller and the file may be left partially
      # written.  The binary mode is important for integrity-sensitive data:
      # no newline or encoding translation is performed.
      #
      # Arguments:
      #   fileName -- The name of the file to create or overwrite.
      #   data     -- The exact bytes to write to the file, with no trailing
      #               newline appended.
      #
      # Results:
      #   The empty string on success; otherwise a script error is raised.
      # </help>

      set channel [open $fileName {WRONLY CREAT TRUNC}]
      makeBinaryChannel $channel
      puts -nonewline $channel $data
      close $channel
      return ""
    }
  }

  #
  # NOTE: This procedure was stolen from the "common.tcl" script.  This
  #       procedure sets up the default values for all HTTP configuration
  #       parameters used by this package.  If the force argument is
  #       non-zero, any existing values will be overwritten and set back
  #       to their default values.
  #
  proc setupCommonVariables { force } {
    # <help>
    # This procedure initializes the namespace variables that control how this
    # package performs HTTP (and HTTPS) requests, establishing the default
    # security posture for all downloads.  It exists so that the rest of the
    # package -- principally [getFileViaHttp] -- can rely on a consistent set
    # of configuration variables being present, and so that those variables
    # have safe-by-default values unless an operator deliberately overrides
    # them.  It is called once when the package is loaded (with force set to
    # false) and can be called again to forcibly reset the security-relevant
    # settings.
    #
    # How it works: each configuration variable is declared with [variable]
    # and then conditionally assigned its default value.  Two distinct
    # initialization rules are used.  For the security-critical transport
    # settings -- forceSecureUri, mustHaveTls, allowInsecureUri, and
    # verboseGetUrl -- the default is (re)applied whenever "force" is true OR
    # the variable does not yet exist; this means a forced call will reset
    # these back to their secure defaults even if an operator had relaxed them.
    # For the remaining settings -- allowInsecureRedirect, timeoutGetUrl -- the
    # default is applied only when the variable does not already exist, so a
    # forced call does NOT reset them.
    #
    # SECURITY: the secure-by-default values are important.  forceSecureUri
    # defaults to true, so plain HTTP URIs are upgraded to HTTPS when the "tls"
    # package is available.  mustHaveTls defaults to true, so a missing "tls"
    # package is treated as a hard error rather than silently falling back to
    # cleartext.  allowInsecureUri defaults to false, so HTTP is not used as a
    # fallback when "tls" is unavailable.  allowInsecureRedirect defaults to
    # false, so an HTTPS request will not be allowed to redirect down to plain
    # HTTP.  Relaxing any of these reduces transport security; the comments in
    # the body also note that the official repository server may refuse plain
    # HTTP regardless, making some insecure settings pointless.  These settings
    # only affect native Tcl behavior.
    #
    # Arguments:
    #   force -- A boolean.  When true, the four transport security settings
    #            (forceSecureUri, mustHaveTls, allowInsecureUri, verboseGetUrl)
    #            are reset to their defaults even if they already exist; the
    #            redirect and timeout settings are still only defaulted when
    #            absent.  When false, every setting is only assigned its default
    #            if it does not already exist.
    #
    # Results:
    #   The result of the final assignment (the default timeout value); callers
    #   invoke this for its side effects on the namespace variables, not for
    #   its return value.
    # </help>

    #
    # NOTE: Should the HTTP request processor attempt to force the use of
    #       HTTPS for URIs that were originally HTTP?  This setting is only
    #       applicable to native Tcl.
    #
    variable forceSecureUri; # DEFAULT: true

    if {$force || ![info exists forceSecureUri]} then {
      set forceSecureUri true
    }

    #
    # NOTE: Should the HTTP request processor fail if the "tls" package is
    #       not available?
    #
    variable mustHaveTls; # DEFAULT: true

    if {$force || ![info exists mustHaveTls]} then {
      set mustHaveTls true
    }

    #
    # NOTE: Is this HTTP request processor allowed to use plain HTTP if/when
    #       the "tls" package is not available?  This should only be changed
    #       if the "tls" package cannot be easily installed for use with the
    #       native Tcl interpreter in use.  It should be noted here that the
    #       official package repository server reserves the right to refuse
    #       plain HTTP connections, which means that changing this setting
    #       may be totally pointless.
    #
    variable allowInsecureUri; # DEFAULT: false

    if {$force || ![info exists allowInsecureUri]} then {
      set allowInsecureUri false
    }

    #
    # NOTE: Emit diagnostic messages when the [::http::geturl] procedure is
    #       about to be called?
    #
    variable verboseGetUrl; # DEFAULT: false

    if {$force || ![info exists verboseGetUrl]} then {
      set verboseGetUrl false
    }

    #
    # NOTE: Is this HTTP request processor allowed to use plain HTTP if/when
    #       the server responds with an HTTP redirect location to an original
    #       URI that was HTTPS?  Otherwise, a script error will result.
    #
    variable allowInsecureRedirect; # DEFAULT: false

    if {![info exists allowInsecureRedirect]} then {
      set allowInsecureRedirect false
    }

    #
    # NOTE: How long should we wait for the HTTP request to complete?  This
    #       value is the number of milliseconds.
    #
    variable timeoutGetUrl; # DEFAULT: 0

    if {![info exists timeoutGetUrl]} then {
      set timeoutGetUrl 0
    }
  }

  #
  # NOTE: This procedure was stolen from the "common.tcl" script.  It is
  #       designed to emit a message to the console.  The channel argument
  #       is the channel where the message should be written.  The string
  #       argument is the content of the message to emit.  If the channel
  #       argument is an empty string, nothing is written.
  #
  proc pageOut { channel string } {
    # <help>
    # This procedure writes a message to a console channel, immediately
    # flushing it, while tolerating any output error.  It exists as the single
    # low-level output primitive used to emit progress indicators and status
    # text to the user during potentially long-running HTTP operations, where a
    # transient write failure should never be allowed to abort the operation.
    #
    # How it works: if the channel argument is a non-empty string, the message
    # is written with [puts -nonewline] (so the caller controls all line
    # breaks) and the channel is flushed so the output appears promptly.  The
    # entire write-and-flush is wrapped in a [catch], so any failure (for
    # example, a closed or redirected channel) is silently ignored.  When the
    # channel argument is the empty string, nothing is written at all, which
    # provides a simple way to disable output.
    #
    # Arguments:
    #   channel -- The output channel to write to, or the empty string to
    #              suppress all output.
    #   string  -- The text to write, verbatim, with no added newline.
    #
    # Results:
    #   The empty string.  Output errors are caught and ignored.
    # </help>

    if {[string length $channel] > 0} then {
      catch {
        puts -nonewline $channel $string; flush $channel
      }
    }
  }

  #
  # NOTE: This procedure was stolen from the "common.tcl" script.  It is
  #       designed to emit a message to the HTTP client log.  The string
  #       argument is the content of the message to emit.  If the string
  #       argument is an empty string, nothing is written.
  #
  proc pageLog { string } {
    # <help>
    # This procedure appends a diagnostic message to the Tcl log, tolerating
    # any logging error.  It exists to record HTTP client activity (for
    # example, the URL and arguments of a request when verbose mode is enabled)
    # in a uniform, timestamped format so that downloads can be diagnosed after
    # the fact.
    #
    # How it works: if the message is a non-empty string, it is passed to
    # [tclLog] prefixed with the current process id, the current wall-clock
    # time in seconds since the epoch, and the literal tag "http", all joined
    # by " : " separators.  The [tclLog] call is wrapped in a [catch] so that a
    # logging failure never interferes with the operation being logged.  An
    # empty message produces no log output.
    #
    # Arguments:
    #   string -- The message to log, or the empty string to log nothing.
    #
    # Results:
    #   The empty string.  Logging errors are caught and ignored.
    # </help>

    if {[string length $string] > 0} then {
      catch {
        tclLog [appendArgs \
            [pid] " : " [clock seconds] " : http : " $string]
      }
    }
  }

  #
  # NOTE: This procedure was stolen from the "common.tcl" script.  It is
  #       designed to setup the pending progress indicator callback and
  #       save its working state.
  #
  proc setupPageProgress { channel type milliseconds } {
    # <help>
    # This procedure schedules the next periodic progress-indicator callback
    # and records the resulting event identifier so it can later be cancelled.
    # It exists to drive the animated progress display shown while an HTTP
    # request is in flight, working together with [pageProgress] (which it
    # invokes) and [cancelPageProgress] (which tears it down).
    #
    # How it works: it uses [after] to schedule a call to [pageProgress] after
    # the given number of milliseconds, wrapping the callback in [namespace
    # code] so that it executes in this namespace when the event fires.  The
    # token returned by [after] is stored in the namespace variable
    # afterForPageProgress, which serves as the single record of the pending
    # event.
    #
    # Tricky details: this procedure overwrites afterForPageProgress without
    # first cancelling any event it may already hold, so callers are expected
    # to ensure (typically via [cancelPageProgress]) that no event is already
    # pending; otherwise the previously scheduled event would be orphaned and
    # could no longer be cancelled through the saved token.
    #
    # Arguments:
    #   channel      -- The output channel passed through to [pageProgress] for
    #                   emitting the indicator.
    #   type         -- The single-character progress indicator to emit.
    #   milliseconds -- The delay, in milliseconds, before the callback fires;
    #                   also forwarded so the callback can reschedule itself.
    #
    # Results:
    #   The [after] event identifier of the newly scheduled callback (also
    #   stored in afterForPageProgress).
    # </help>

    #
    # NOTE: This variable is used to keep track of the currently scheduled
    #       (i.e. pending) [after] event.
    #
    variable afterForPageProgress

    #
    # NOTE: Scheduled the necessary [after] event, using the [pageProgress]
    #       procedure, which is defined further down in this file.
    #
    set afterForPageProgress [after $milliseconds [namespace code \
        [list pageProgress $channel $type $milliseconds]]]
  }

  #
  # NOTE: This procedure was stolen from the "common.tcl" script.  It is
  #       designed to cancel the pending progress indicator callback and
  #       cleanup its working state.
  #
  proc cancelPageProgress {} {
    # <help>
    # This procedure cancels any pending progress-indicator callback and clears
    # the bookkeeping that tracks it.  It exists to tear down the animated
    # progress display once an HTTP request has completed (or before
    # rescheduling), so that no stray [after] event continues to fire after the
    # work is done.
    #
    # How it works: if the namespace variable afterForPageProgress exists, the
    # event it identifies is cancelled with [after cancel] (wrapped in a
    # [catch] so that an already-fired or unknown event is harmless), and the
    # variable is then removed with [unset -nocomplain].  If the variable does
    # not exist, nothing is done.
    #
    # Arguments:
    #   None.
    #
    # Results:
    #   The empty string.  Safe to call when no progress callback is pending.
    # </help>

    #
    # NOTE: This variable is used to keep track of the currently scheduled
    #       (i.e. pending) [after] event.
    #
    variable afterForPageProgress

    #
    # NOTE: If there is a currently scheduled [after] event, cancel it.
    #
    if {[info exists afterForPageProgress]} then {
      catch {after cancel $afterForPageProgress}
      unset -nocomplain afterForPageProgress
    }
  }

  #
  # NOTE: This procedure was stolen from the "common.tcl" script.  It is
  #       designed to emit a progress indicator while an HTTP request is
  #       being processed.  The channel argument is the Tcl channel where
  #       the progress indicator should be emitted.  The type argument is
  #       the single-character progress indicator.  The milliseconds
  #       argument is the number of milliseconds to wait until the next
  #       periodic progress indicator should be emitted.  This procedure
  #       reschedules its own execution.
  #
  proc pageProgress { channel type milliseconds } {
    # <help>
    # This procedure emits one progress indicator and, if requested, reschedules
    # itself to run again, producing the periodic "something is still happening"
    # display seen during HTTP downloads and redirects.  It exists as the
    # callback body invoked by the [after] events set up in [setupPageProgress].
    #
    # How it works: it first writes the indicator character to the channel via
    # [pageOut].  It then calls [cancelPageProgress] to ensure no other progress
    # event is pending, and -- only if the milliseconds value is greater than
    # zero -- calls [setupPageProgress] to schedule the next invocation.  This
    # self-rescheduling design is what makes the indicator repeat at a fixed
    # interval; passing zero (or a negative value) emits a single indicator
    # without scheduling a follow-up.
    #
    # Arguments:
    #   channel      -- The output channel on which to emit the indicator (may
    #                   be the empty string to suppress output, per [pageOut]).
    #   type         -- The single-character progress indicator to emit.
    #   milliseconds -- The interval before the next indicator; when greater
    #                   than zero the callback reschedules itself, otherwise it
    #                   does not.
    #
    # Results:
    #   The empty string (the result of [setupPageProgress] or of the [if]).
    #   The visible effect is one emitted indicator plus, optionally, a newly
    #   scheduled callback.
    # </help>

    #
    # NOTE: Show that something is happening...
    #
    pageOut $channel $type

    #
    # NOTE: Make sure that we are scheduled to run again, if requested;
    #       also, before doing that, make sure there is not already an
    #       associated [after] event pending.
    #
    cancelPageProgress

    if {$milliseconds > 0} then {
      setupPageProgress $channel $type $milliseconds
    }
  }

  #
  # NOTE: This procedure was stolen from the "common.tcl" script.  It is
  #       designed to process a single HTTP request, including any HTTP
  #       3XX redirects (up to the specified limit), and return the raw
  #       HTTP response data.  It may raise any number of script errors.
  #
  # <public>
  proc getFileViaHttp { uri redirectLimit channel quiet args } {
    # <help>
    # This procedure performs a single logical HTTP(S) download -- following
    # any HTTP 3XX redirects up to a caller-specified limit -- and returns the
    # raw response body.  It is the security-critical transport workhorse of
    # this package: every package archive, OpenPGP key, and signature file is
    # fetched through it.  It is a public, exported entry point.
    #
    # How it works: it requires the modern "http" package and, on Tcl 8.6 or
    # higher, forces IPv4 sockets (unless ::no(tclSocketAfInet) is set) to
    # avoid long IPv6 connection hangs seen on some hosts.  It then attempts to
    # load the "tls" package and, on success, registers it as the HTTPS
    # transport on port 443 using TLS 1.x.  It performs the request inside a
    # loop: it builds the option list (adding -timeout when timeoutGetUrl is
    # non-zero and appending any caller-supplied "args"), calls
    # [::http::geturl], checks that [::http::status] is "ok", reads the numeric
    # response code and body, and dispatches on the code.  Informational (1XX),
    # unsupported-redirect (300, 304, 305, 306), client-error (4XX), and
    # server-error (5XX) codes all raise a script error after cleaning up the
    # token.  Success codes (200-208, 226) break out of the loop and the body
    # is returned.  Redirect codes (301, 302, 303, 307, 308) cause the
    # Location header to be read and used as the URI for the next iteration.
    # Throughout, progress indicators are emitted via [pageProgress] unless the
    # caller requested quiet mode, and HTTP tokens are cleaned up on every path
    # to avoid leaks.
    #
    # SECURITY: this procedure embodies the package's transport trust model,
    # governed by the namespace variables set up by [setupCommonVariables].
    # When "tls" loads and forceSecureUri is true, an http:// URI is rewritten
    # to https:// before the request.  When "tls" cannot be loaded, behavior
    # depends on mustHaveTls (true by default -- a hard error is raised) and
    # allowInsecureUri (false by default -- plain HTTP is refused; if enabled,
    # an https:// URI may be downgraded to http://).  Redirects are scrutinized:
    # unless allowInsecureRedirect is true, a redirect from an https:// URI to a
    # non-https:// location is refused with an error, preventing a silent HTTPS
    # to HTTP downgrade via a malicious or misconfigured redirect.  An empty
    # response code is treated as an error because it has been observed to
    # indicate a broken tls/http stack (notably tls 1.6.1 on macOS) rather than
    # a trustworthy result.  Note carefully what this procedure does NOT do: it
    # establishes a secure transport and validates the response code, but it
    # performs NO authentication of the content itself -- it does not verify
    # OpenPGP signatures, checksums, or certificate pinning here.  Content
    # integrity and authenticity are the responsibility of higher layers (for
    # example, the OpenPGP verification performed by the repository client).
    # The string scheme comparisons are length-bounded and case-insensitive.
    #
    # Tricky details: all I/O is synchronous (simple but blocking).  A
    # redirectLimit of zero forbids all redirects, and any negative value
    # disables the limit entirely (unlimited redirects).  Each redirect creates
    # a fresh token; the previous one is always cleaned up first.
    #
    # Arguments:
    #   uri           -- The URI to download.  May be rewritten between http
    #                    and https per the security settings described above.
    #   redirectLimit -- Maximum number of redirects to follow: zero disallows
    #                    redirects, a negative value allows unlimited redirects,
    #                    and a positive value caps the count before an error.
    #   channel       -- The output channel for progress indicators, or the
    #                    empty string; ignored when quiet is true.
    #   quiet         -- A boolean; when true, suppresses all progress output
    #                    and the associated [after] bookkeeping.
    #   args          -- Optional additional options appended verbatim to the
    #                    [::http::geturl] call (for example, extra headers or
    #                    method/query options).
    #
    # Results:
    #   The raw HTTP response body (which may be empty) on a successful (2XX)
    #   response.  Otherwise a script error is raised describing the failure
    #   (bad status, empty/unsupported/error response code, refused insecure
    #   redirect, missing redirect location, exceeded redirect limit, or an
    #   unavailable "tls" package when it is required).
    # </help>

    #
    # NOTE: This global variable is used to check the running version of
    #       Tcl.
    #
    global tcl_version

    #
    # NOTE: This variable is used to determine if plain HTTP URIs should be
    #       converted to HTTPS, if the "tls" package is available.
    #
    variable forceSecureUri

    #
    # NOTE: This variable is used to determine if an error should be raised
    #       if the "tls" package is not available.
    #
    variable mustHaveTls

    #
    # NOTE: This variable is used to determine if plain HTTP is allowed if
    #       the "tls" package is not available.
    #
    variable allowInsecureUri

    #
    # NOTE: This variable is used to determine if a diagnostic message is
    #       emitted when [::http::geturl] is about to be called.
    #
    variable verboseGetUrl

    #
    # NOTE: This variable is used to determine if plain HTTP is allowed if
    #       an HTTP redirect response contains an HTTP URI and the original
    #       URI was HTTPS.
    #
    variable allowInsecureRedirect

    #
    # NOTE: This variable is used to determine the timeout milliseconds for
    #       HTTP requests.
    #
    variable timeoutGetUrl

    #
    # NOTE: This procedure requires the modern version of the HTTP package,
    #       which is typically included with the Tcl core distribution.
    #
    package require http 2.0

    #
    # NOTE: Tcl 8.6 added support for IPv6; however, on some machines this
    #       support can cause sockets to hang for a long time.  Therefore,
    #       for now, by default, always force the use of IPv4.
    #
    if {![info exists ::no(tclSocketAfInet)] && \
        [info exists tcl_version] && $tcl_version >= 8.6} then {
      namespace eval ::tcl::unsupported {}
      set ::tcl::unsupported::socketAF inet
    }

    #
    # NOTE: Setup lowercase URI scheme prefixes used within this procedure
    #       to detect and/or change the URI scheme used.  By default, this
    #       procedure will always attempt to force HTTPS use when the "tls"
    #       package is available -AND- it disallows redirects from HTTPS to
    #       HTTP -AND- it disallows using HTTP when the "tls" package is
    #       unavailable.
    #
    set http http://
    set httpLen [string length $http]
    set httpEnd [expr {$httpLen - 1}]

    set https https://
    set httpsLen [string length $https]
    set httpsEnd [expr {$httpsLen - 1}]

    #
    # NOTE: If the "tls" package is available, always attempt to use HTTPS;
    #       otherwise, only attempt to use HTTP if explicitly allowed.
    #
    if {[catch {package require tls} error] == 0} then {
      ::http::register https 443 [list ::tls::socket -tls1 true]

      if {$forceSecureUri} then {
        if {[string tolower [string range $uri 0 $httpEnd]] eq $http} then {
          set uri [appendArgs $https [string range $uri $httpLen end]]
        }
      }
    } else {
      if {$mustHaveTls} then {
        error [appendArgs \
            "the \"tls\" package cannot be loaded: " $error]
      }

      if {$allowInsecureUri} then {
        if {[string tolower [string range $uri 0 $httpsEnd]] eq $https} then {
          set uri [appendArgs $http [string range $uri $httpsLen end]]
        }
      }
    }

    #
    # NOTE: Unless the caller forbids it, display progress messages during
    #       the download.
    #
    if {!$quiet} then {
      pageProgress $channel . 250
    }

    #
    # NOTE: All downloads are handled synchronously, which is not ideal;
    #       however, it is simple.  Keep going as long as there are less
    #       than X redirects.
    #
    set redirectCount 0

    while {1} {
      #
      # NOTE: Build the (optional?) list of options for the HTTP call.
      #
      set localArgs [list]

      if {$timeoutGetUrl != 0} then {
        lappend localArgs -timeout $timeoutGetUrl; # milliseconds
      }

      if {[llength $args] > 0} then {
        eval lappend localArgs $args
      }

      #
      # NOTE: Issue the HTTP request now, grabbing the resulting token.
      #
      if {$verboseGetUrl} then {
        #
        # NOTE: Emit important diagnostic information related to this
        #       HTTP request here.  This may be enhanced in the future.
        #
        pageLog [appendArgs \
            "attempting to download URL \"" $uri "\" with arguments \"" \
            $localArgs \"...]
      }

      #
      # NOTE: Attempt to perform the actual HTTP request.  This can fail
      #       in an almost unlimited number of ways, which is fun.
      #
      set token [eval ::http::geturl [list $uri] $localArgs]

      #
      # NOTE: Grab the HTTP status.  It must be "ok" in order to proceed.
      #
      set status [::http::status $token]

      if {$status ne "ok"} then {
        error [appendArgs \
            "bad HTTP status \"" $status "\" is not \"ok\""]
      }

      #
      # NOTE: Grab the HTTP response code and data now as they are needed
      #       in almost all cases.
      #
      set code [::http::ncode $token]; set data [::http::data $token]

      #
      # NOTE: If the HTTP response code is an empty string that may
      #       indicate a serious bug in the tls (or http) package for
      #       this platform.  So far, this issue has only been seen
      #       with the tls 1.6.1 package that shipped with macOS.
      #
      if {[string length $code] == 0} then {
        error [appendArgs \
            "received empty HTTP response code for URL \"" $uri \
            "\", the \"tls\" (and/or \"http\") package(s) may be " \
            "broken for this Tcl installation (or platform)"]
      }

      #
      # NOTE: Check the HTTP response code, in order to follow any HTTP
      #       redirect responses.
      #
      switch -glob -- $code {
        100 -
        101 -
        102 {
          ::http::cleanup $token; error [appendArgs \
              "unsupported informational HTTP response status code " \
              $code ", data: " $data]
        }
        200 -
        201 -
        202 -
        203 -
        204 -
        205 -
        206 -
        207 -
        208 -
        226 {
          #
          # NOTE: Ok, the HTTP response is actual data of some kind (which
          #       may be empty).
          #
          ::http::cleanup $token; break
        }
        301 -
        302 -
        303 -
        307 -
        308 {
          #
          # NOTE: Unless the caller forbids it, display progress messages
          #       when an HTTP redirect is returned.
          #
          if {!$quiet} then {
            pageProgress $channel > 0
          }

          #
          # NOTE: We hit another HTTP redirect.  Stop if there are more
          #       than X.
          #
          incr redirectCount

          #
          # TODO: Maybe make this limit more configurable?  The caller
          #       can pass any negative integer to disable it entirely
          #       -OR- zero to completely disallow any redirects.
          #
          if {$redirectLimit >= 0 && \
              $redirectCount > $redirectLimit} then {
            #
            # NOTE: Just "give up" and raise a script error.
            #
            ::http::cleanup $token; error [appendArgs \
                "redirection limit of " $redirectLimit " exceeded"]
          }

          #
          # NOTE: Grab the metadata associated with this HTTP response.
          #
          unset -nocomplain meta; array set meta [::http::meta $token]

          #
          # NOTE: Is there actually a new URI (location) to use?
          #
          if {[info exist meta(Location)]} then {
            #
            # NOTE: Ok, grab it now.  Later, at the top of the loop,
            #       it will be used in the subsequent HTTP request.
            #
            set location $meta(Location); unset meta

            #
            # NOTE: For security, by default, do NOT follow an HTTP
            #       redirect if it attempts to redirect from HTTPS
            #       to HTTP.
            #
            if {!$allowInsecureRedirect && \
                [string tolower [string range \
                    $uri 0 $httpsEnd]] eq $https && \
                [string tolower [string range \
                    $location 0 $httpsEnd]] ne $https} then {
              #
              # NOTE: Just "give up" and raise a script error.
              #
              ::http::cleanup $token; error [appendArgs \
                  "refused (insecure) redirect from \"" $uri \
                  "\" to \"" $location \
                  "\" with HTTP response status code " $code]
            }

            #
            # NOTE: Replace the original URI with the new one, for
            #       use in the next HTTP request.
            #
            set uri $location

            #
            # NOTE: Cleanup the current HTTP token now beause a new
            #       one will be created for the next request.
            #
            ::http::cleanup $token
          } else {
            #
            # NOTE: Just "give up" and raise a script error.
            #
            ::http::cleanup $token; error [appendArgs \
                "redirect from \"" $uri \
                "\" missing location, HTTP response status code " \
                $code ", data: " $data]
          }
        }
        300 -
        304 -
        305 -
        306 {
          ::http::cleanup $token; error [appendArgs \
              "unsupported redirection HTTP response status code " \
              $code ", data: " $data]
        }
        4?? {
          ::http::cleanup $token; error [appendArgs \
              "client error HTTP response status code " $code ", data: " \
              $data]
        }
        5?? {
          ::http::cleanup $token; error [appendArgs \
              "server error HTTP response status code " $code ", data: " \
              $data]
        }
        default {
          ::http::cleanup $token; error [appendArgs \
              "unrecognized HTTP response status code " $code ", data: " \
              $data]
        }
      }
    }

    #
    # NOTE: If there is a currently scheduled [after] event, cancel it.
    #       This is NOT done if the caller enabled quiet mode, because
    #       there should be none of our [after] events present in that
    #       case.
    #
    if {!$quiet} then {
      cancelPageProgress
    }

    #
    # NOTE: If progress messages were emitted, start a fresh line.
    #
    if {!$quiet} then {
      pageOut $channel [appendArgs " " $uri \n]
    }

    return $data
  }

  #
  # NOTE: First, setup the variables associated with this package.
  #
  setupCommonVariables false

  #
  # NOTE: Export the procedures from this namespace that are designed to be
  #       used by external scripts.
  #
  namespace export appendArgs getFileViaHttp pageOut writeFile

  #
  # NOTE: Provide the package to the interpreter.
  #
  package provide Eagle.Tools.Common 1.0
}
