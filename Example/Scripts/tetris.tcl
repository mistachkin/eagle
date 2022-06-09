#!/bin/sh
# the next line starts with wish, don't remove the slash --> \
exec wish "$0" ${1+"$@"}

##----------------------------------------------------------------------
## tetris.tcl
##
## Copyright (c) 1995-1999 Jeffrey Hobbs
## Started: Fri Aug 25 15:27:32 PDT 1995
##
## jeff.hobbs@acm.org
##
## If you'd like to discuss any aspects of the program, I welcome your email.
## If anyone improves upon this, please email changes to me.
##
## source standard_disclaimer.tcl
## source bourbon_ware.tcl
##
## Tetris with multiplayer mode (Netris!).  Requires Tcl/Tk 8.0+.
## Works either in Tk plugin or stand-alone.
## See HTML docs for more info.
##----------------------------------------------------------------------

package require Tk
namespace eval Tetris {;

variable tetris
variable block
variable players
variable stats
variable pmap
variable piece
variable widget

## VERSION:
set tetris(version) 3.2

set tetris(WWW) [info exists embed_args]
if {$tetris(WWW)} {
    proc bell args {}
    proc Usage {} {}
} else {
    wm withdraw .

    proc Usage {} {
	global argv0
	puts stderr "Usage: [file tail $argv0] ?-info? ?-blocksize #pixels?\
		?-autopause 0/1? ?-color\[0-6\] rgbcolor?\
		?-maxinterval #usecs? ?-shownext 0/1?\
		?-(left|right|rotleft|rotright|drop|slide) binding?"
	exit
    }

    proc About {} {
	variable tetris
	tk_dialog $tetris(base).about "About Tetris v$tetris(version)" \
		$tetris(info) questhead 0 OK
    }
}

proc ParseArgs {} {
    variable tetris
    variable keys
    variable color

    if {$tetris(WWW)} {
	global embed_args
	set args [array get embed_args]
    } else {
	global argv
	set args $argv
    }
    set truth {^(1|yes|true|on)$}
    for {set i 0; set argc [llength $args]} {$i<$argc} {incr i} {
	set key [string tolower [string triml [lindex $args $i] -]]
	set val [lindex $args [incr i]]
	switch -glob $key {
	    width - height - src { continue }
	    a*		{ set tetris(autoPause)	[regexp -nocase $truth $val] }
	    b*		{ set tetris(blocksize)	$val }
	    color[0-6] {
		if {[regexp {([0-6])} $key junk num]} {
		    set color($num) $val
		}
	    }
	    dr*		{ set keys(Drop)	$val }
	    growi*	{ set tetris(growing)	[regexp -nocase $truth $val] }
	    growl*	{ set tetris(growLevel)	$val }
	    growm*	{ set tetris(growMax)	$val }
	    ma*		{ set tetris(maxInterval)	$val }
	    lef*	{ set keys(Left)	$val }
	    lev*	{ set tetris(initLevel)	$val }
	    ri*		{ set keys(Right)	$val }
	    rotl*	{ set "keys(Rotate Left)"	$val }
	    rotr*	{ set "keys(Rotate Right)"	$val }
	    sha*	{ set tetris(shadow)	[regexp -nocase $truth $val] }
	    sho*	{ set tetris(showNext)	[regexp -nocase $truth $val] }
	    sl*		{ set keys(Slide)	$val }

	    mu*		{ set tetris(multi)	[regexp -nocase $truth $val] }
	    hi*		{ set tetris(highband)	[regexp -nocase $truth $val] }
	    ho*		{ array set tetris [list host $val con,host $val] }
	    p*		{ array set tetris [list port $val con,port $val] }

	    help - info - default Usage
	}
    }
}

proc Init {{base {}}} {
    variable tetris
    variable widget
    variable keys
    variable color

    set tetris(name) [namespace current]::
    array set tetris {
	blocksize	15
	maxInterval	500
	initLevel	0
	autoPause	0
	showNext	1
	shadow		1
	growing		0
	growLevel	0
	growMax		26

	highband	1
	multi		1
	port		15141
	con,port	15141
	sockets		0
	sync		0

	maxbrick 0 numplayers 0 deaths 0
	growRows 0 growInterval idle

	info	"(c) Jeffrey Hobbs 1995-1999\njeff.hobbs@acm.org"
    }
    array set color {
	0		\#FF0000
	1		\#00FF00
	2		\#0000FF
	3		\#FFFF00
	4		\#FF00FF
	5		\#00FFFF
	6		\#FFFFFF
    }
    array set keys {
	Left		<Key-Left>
	Right		<Key-Right>
	"Rotate Left"	<Key-Up>
	"Rotate Right"	<Key-space>
	Slide		<Key-Return>
	Drop		<Key-Down>
	Options		<Key-o>
	Quit		<Control-q>
	Start		<Key-s>
	Reset		<Key-r>
	AddRow		<Key-a>
	Faster		<Key-f>
	MultiPlayer	<Control-m>
	GameTypes	<Control-g>
	Stats		<Control-s>
	About		<Control-a>
	Keys		<Control-k>
    }

    option add *Button.takeFocus	0 startup
    option add *Button*highlightThickness 1 startup
    option add *Canvas*highlightThickness 0 startup
    option add *Canvas.borderWidth	1 startup
    option add *Canvas.relief		ridge startup

    ParseArgs

    foreach i [array names keys] {
	regsub { } $i {} event
	event add <<$event>> $keys($i)
    }

    set bs $tetris(blocksize)
    array set tetris [list root $base base $base host [info hostname] \
	    con,host	[info hostname] myip 127.0.0.1 \
	    width	[expr {10*$bs}] \
	    height	[expr {30*$bs}]]
    if {[string match {} $base]} { set tetris(root) . }

    set left [frame $base.l]
    set right [frame $base.r]
    array set widget [list board	$right.board \
	    stats	$base.stats	shadow	$right.shade \
	    next	$left.next	opts	$base.opts \
	    multi	$base.multi	player	$base.multi.pla \
	    socks	$base.socks	keys	$base.keys \
	    games	$base.game \
	    ]
    frame $widget(socks)

    grid $left $right $widget(socks) -sticky new
    grid configure $widget(socks) -sticky news

    canvas $widget(board) -width $tetris(width) -height $tetris(height) \
	    -bg gray
    canvas $widget(shadow) -width $tetris(width) -height $bs \
	    -bg gray

    # Oh my gosh, a cheat!
    $widget(board) bind piece <<Cheater>> [namespace code {Cheat %W}]
    $widget(board) bind struc <<Cheater>> [namespace code {Cheat %W}]
    event add <<Cheater>> <Triple-1>

    label $left.title -text "Tetris v$tetris(version)" -relief ridge -bd 2
    label $left.lnext -text "Next Object" -anchor c
    canvas $widget(next) -width [expr {$bs*4+10}] -height [expr {$bs*2+10}]
    button $left.start -textvariable $tetris(name)tetris(start) \
	    -command [namespace code ToggleState]
    button $left.reset -text "Reset" -un 0 -command [namespace code {
	Reset
	if {$tetris(numplayers)} {
	    incr tetris(deaths)
	    TellPlayers update 0 $tetris(maxbrick) $tetris(deaths)
	}
    }
    ]
    button $left.addrow -text "Add Random Row" -underline 0 \
	    -command [namespace code {AddRows 1}]

    if {!$tetris(WWW)} {
	button $left.quit  -text "Quit" -underline 0 \
		-command [namespace code exit]
	button $left.opts -text "Options" -underline 0 \
		-command [namespace code Options]
	checkbutton $left.pause -text "Auto Pause" -anchor w \
		-variable $tetris(name)tetris(autoPause) \
		-command [namespace code AutoPause]

	bind $tetris(root) <<Options>>		[list $left.opts invoke]
	bind $tetris(root) <<Quit>>		[list $left.quit invoke]

	foreach i {MultiPlayer GameTypes Stats About Keys} {
	    bind $tetris(root) <<$i>>	[namespace code $i]
	}
    }

    checkbutton $left.show -text "Show Next" -anchor w \
	    -variable $tetris(name)tetris(showNext) \
	    -command [namespace code ShowNext]
    checkbutton $left.shadow -text "Shadow Piece" -anchor w \
	    -variable $tetris(name)tetris(shadow) \
	    -command [namespace code Shadow]
    checkbutton $left.grow -text "Growing Rows" -anchor w \
	    -variable $tetris(name)tetris(growing) \
	    -command [namespace code GrowRows]
    label $left.glbl -text "Grow Speed:" -anchor w
    button $left.gup -text "^" -padx 0 -pady 0 -command [namespace code \
	    {set tetris(growLevel) [expr {($tetris(growLevel)+1)%16}]}]
    label $left.gval -textvariable $tetris(name)tetris(growLevel) -anchor e
    label $left.mlbl -text "Grow To Max:" -anchor w
    button $left.mup -text "^" -padx 0 -pady 0 -command [namespace code \
	    {set tetris(growMax) [expr {($tetris(growMax)+1)%27}]}]
    label $left.mval -textvariable $tetris(name)tetris(growMax) -anchor e

    label $left.lscore -text Score: -anchor w
    label $left.llvl -text Level: -anchor w
    label $left.lrows -text Rows:   -anchor w
    label $left.vscore -textvariable $tetris(name)tetris(score) -anchor e
    label $left.vlvl -textvariable $tetris(name)tetris(level) -anchor e
    label $left.vrows -textvariable $tetris(name)tetris(rows) -anchor e
    button $left.ilvl -text "^" -padx 0 -pady 0 -command [namespace code \
	    {SetIntervalLevel [incr tetris(level)]}]

    bind $tetris(root) <<Start>> [list $left.start invoke]
    bind $tetris(root) <<Reset>> [list $left.reset invoke]
    bind $tetris(root) <<AddRow>> [list $left.addrow invoke]
    bind $tetris(root) <<Faster>> [list $left.ilvl invoke]

    grid $left.title	- - -sticky new
    grid $left.lnext	- - -sticky new
    grid $widget(next)	- - -sticky n
    grid $left.lscore $left.vscore - -sticky new
    grid $left.llvl   $left.ilvl $left.vlvl -sticky new
    grid $left.lrows  $left.vrows - -sticky new
    grid $left.start	- - -sticky new
    grid $left.reset	- - -sticky new
    grid $left.addrow	- - -sticky new

    if {!$tetris(WWW)} {
	grid $left.opts		- - -sticky new
	grid $left.quit		- - -sticky new
	grid $left.pause	- - -sticky new
    }
    grid $left.show	- - -sticky new
    grid $left.shadow	- - -sticky new
    grid $left.grow	- - -sticky new
    grid $left.glbl $left.gup $left.gval -sticky new
    grid $left.mlbl $left.mup $left.mval -sticky new

    grid configure $left.llvl $left.ilvl $left.gup $left.mup \
	    $left.lrows $left.lscore -sticky nw

    grid $widget(board) -sticky news
    grid $widget(shadow) -sticky news

    ## Don't touch this - the returned canvas id numbers are important
    for {set j 0} {$j < 30} {incr j} {
	for {set i 0} {$i < 10} {incr i} {
	    set x [expr {int(($i+.5)*$bs)}]
	    set y [expr {int(($j+.5)*$bs-1)}]
	    $widget(board) create line $x $y $x [incr y 2] \
		    -tags back -fill $color(0)
	    if {$j == 0} {
		$widget(shadow) create rect [expr {$i*$bs}] 0 \
			[expr {($i+1)*$bs}] $bs -outline {}
	    }
	}
    }
    focus $widget(board)

    Reset
    InitPieces $bs
    if {$tetris(WWW)} {
	Stats
	Keys
    } else {
	wm resizable $tetris(root) 0 0
	wm deiconify $tetris(root)
    }
    AutoPause
    if {!$tetris(WWW) && $tetris(multi)} {
	if {[catch SocketInit msg]} { bgerror $msg }
    }
}

proc SocketInit {} {
    variable tetris
    variable widget

    set port $tetris(port)
    ## Tries to connect to the specified port or any of the 50 above it
    while {[catch {socket -server [namespace code SocketAccept] $port} msg]} {
	if {[string match {*address already in use} $msg]} {
	    if {[incr port]>[expr {$tetris(port)+50}]} {
		set tetris(multi) 0
		return -code error "unable to find available port"
	    }
	} else {
	    set tetris(multi) 0
	    return -code error $msg
	}
    }
    set tetris(port) $port

    ## Capture our ip/port by sending ourselves a message
    set tetris(sockettest) 1
    set sock [socket -myaddr $tetris(host) $tetris(host) $tetris(port)]
    catch {close $sock}
}

proc SocketAccept {sock ipaddr port} {
    variable tetris
    variable players

    if {[info exists tetris(sockettest)]} {
	unset tetris(sockettest)
	set tetris(myip) $ipaddr
	catch {close $sock}
	return
    }
    set newhost [lindex [fconfigure $sock -peer] 1]
    if {[tk_dialog $tetris(base).socket "Socket Connection" \
	    "$tetris(port): Accept new connection from $newhost ($ipaddr)?" \
	    questhead 0 "You bet" "Kick 'em off"]} {
	puts $sock "Tetris3 $tetris(host) $tetris(port) goodbye"
	flush $sock
	catch {close $sock}
	return
    } else {
	set id "(127\.0\.0\.1|$tetris(host)|localhost)"
	if {[regexp $id $ipaddr]} { set ipaddr $tetris(myip) }
	NewPlayer $sock $newhost $ipaddr $port
    }
}

proc SocketRead {sock} {
    if {[eof $sock] || ([gets $sock line] == -1)} {
	SocketShutdown $sock
	return
    }
    variable tetris
    variable players
    set peer [fconfigure $sock -peer]
    set id "[lindex $peer 0] [lindex $peer 2]"
    ## Line should be of the form:
    ## Tetris<VERSION> <PLAYERHOST> <PLAYERPORT> <SWITCH> args
    ## The sync occurs in Drop, where we don't want to lose our
    ## piece and have the next immediately drop
    #while {$tetris(sync)} {after 4}
    if {$tetris(sync)} {vwait tetris(sync)}
    switch -exact -- [lindex $line 3] {
	pause {
	    Pause
	    set tetris(message) $line
	}
	start {
	    if {$tetris(break)} {
		Resume
		set tetris(message) [lreplace $line 0 0]
	    }
	}
	brick { set players($id,maxbrick) [lindex $line 4] }
	update {
	    if {[scan [lrange $line 4 end] "%d %d %d" \
		    addrows height deaths]==3} {
		AddRows $addrows
		set players($id,maxbrick) $height
		set players($id,deaths) $deaths
	    }
	}
	says { set tetris(message) [lreplace $line 0 0] }
	goodbye { SocketShutdown $sock }
	default {}
    }
}

proc SocketConnect {host port} {
    variable tetris
    variable players

    set id "(127\.0\.0\.1|$tetris(myip)|$tetris(host)|localhost)"
    if {[regexp "$id $tetris(port)" "$host $port"]} {
	return -code error "don't touch yourself"
    }
    if {[catch {socket -myaddr $tetris(host) $host $port} sock]} {
	return -code error $sock
    } else {
	set peer [fconfigure $sock -peer]
	NewPlayer $sock [lindex $peer 1] [lindex $peer 0] $port
    }
}

proc SocketShutdown sock {
    variable tetris
    variable players
    variable widget

    catch {close $sock}

    ## Delete multi-player info
    catch {
	set id $players(sock,$sock)
	catch {pack forget $widget($sock)}
	destroy $widget($sock)
	unset players(sock,$sock) widget($sock)
	catch {unset players($id,sock) players($id,name) tetris($id,maxbrick)}
    }
    set tetris(numplayers) [llength [array names players *,sock]]

    UpdatePlayers
    Pause
    tk_dialog $tetris(base).goodbye "Player left" \
	    "Someone departed..." warning 0 Bummer
    Resume
}

proc NewPlayer {sock host ip port} {
    variable tetris
    variable players
    variable widget

    set id [list $ip $port]
    set players(sock,$sock) $id
    set players($id,sock) $sock
    set players($id,name) [list $host $ip $port]
    fconfigure $sock -blocking 0 -translation {auto crlf}
    fileevent $sock readable [namespace code [list SocketRead $sock]]

    set players($id,maxbrick) 0
    set players($id,deaths) 0

    set widget($sock) $widget(socks).$sock
    destroy $widget($sock)
    set w [frame $widget($sock)]
    scale $w.s -orient v -from 30 -to 0 -state disabled -showvalue yes \
	    -variable $tetris(name)players($id,maxbrick)
    pack $widget($sock) $w.s -fill y -expand 1
    pack [label $w.l -textvariable $tetris(name)players($id,deaths)] -fill both

    set tetris(numplayers) [llength [array names players *,sock]]

    UpdatePlayers
}

proc TellPlayers {args} {
    variable tetris
    variable players

    foreach p [array names players *,sock] {
	if {[catch {
	    set msg "Tetris3 $tetris(host) $tetris(port) $args"
	    puts $players($p) $msg
	    flush $players($p)
	} err]} {
	    SocketShutdown $players($p)
	}
    }
}

proc UpdatePlayers {args} {
    variable players
    variable widget

    if {[winfo exists $widget(player)]} {
	$widget(player) delete 0 end
	foreach p [array names players *,name] {
	    $widget(player) insert end $players($p)
	}
    }
}

proc MultiPlayer {} {
    variable tetris
    variable widget

    set w $widget(multi)
    if {![winfo exists $w]} {
	toplevel $w
	wm withdraw $w
	wm title $w "Tetris v$tetris(version) MultiPlayer"

	label $w.hostl -text "My Host:" -anchor w -width 8
	label $w.host  -textvariable $tetris(name)tetris(host) -anchor e
	label $w.ip    -textvariable $tetris(name)tetris(myip) -anchor w
	label $w.portl -text "My Port:" -anchor w -width 8
	label $w.port  -text $tetris(port) -anchor e -width 8

	label $w.conhl -text "New Host:" -anchor w -width 8
	entry $w.conhe -width 12 -textvariable $tetris(name)tetris(con,host)
	label $w.conpl -text "Port:" -anchor w -width 8
	entry $w.conpe -width 8 -textvariable $tetris(name)tetris(con,port)

	set widget(player) [listbox $w.pla -height 3]
	label $w.lsay -text Message:
	entry $w.say
	bind $w.say <Return> [namespace code { TellPlayers says [%W get] }]
	label $w.msg -textvariable $tetris(name)tetris(message) \
		-relief ridge -bd 2

	checkbutton $w.high -text "High Bandwidth" \
		-variable $tetris(name)tetris(highband)
	button $w.con -text Connect -command [namespace code {
	    SocketConnect $tetris(con,host) $tetris(con,port)
	}]
	button $w.gby -text Disconnect -command [namespace code {
	    TellPlayers goodbye
	}]
	button $w.dis -text Dismiss -command [list wm withdraw $w]

	grid $w.hostl $w.host $w.ip $w.portl $w.port -sticky news
	grid $w.conhl $w.conhe - $w.conpl $w.conpe -sticky news
	grid $w.con - - $w.high - -sticky news
	grid $w.pla - - - - -sticky news
	grid $w.lsay $w.say - - - -sticky news
	grid $w.msg - - - - -sticky news
	grid $w.gby - x $w.dis - -sticky news
	update idletasks
	set a $tetris(root)
	wm transient $w $a
	wm group $w $a
	wm geometry $w +[expr {[winfo rootx $a]+([winfo width $a]\
		-[winfo reqwidth $w])/2}]+[expr {[winfo rooty $a]\
		+([winfo height $a]-[winfo reqheight $w])/2}]
    }
    UpdatePlayers

    if {[string comp normal [wm state $w]]} {
	wm deiconify $w
    } else {
	wm withdraw $w
    }
}

proc SetIntervalLevel {n} {
    variable tetris

    set i [expr {round($tetris(maxInterval)-($tetris(maxInterval)/20*$n))}]
    if {$i<8} { set i 8 }
    set tetris(interval) $i
    set tetris(growInterval) [expr {$tetris(maxInterval) * \
	    (25-$tetris(growLevel))}]
}

proc ToggleState {} {
    variable tetris

    if {[string match $tetris(start) "Pause"]} {
	Pause
	TellPlayers pause
    } elseif {[string match $tetris(start) "Game Over"]} {
	Reset
    } else {
	Resume
	TellPlayers start
    }
}

proc AutoPause {} {
    variable tetris

    if {$tetris(autoPause) && !$tetris(WWW)} {
	## Not available for WWW play
	bind $tetris(root) <Unmap> [namespace code {
	    if {[string match $tetris(start) "Pause"]} { Pause }
	}]
	bind $tetris(root) <Map>   [namespace code {
	    if {[string match $tetris(start) "Resume"]} { Resume }
	}]
	## These are not available for during multiplayer mode
	## (would potentially require way too much communication)
	bind $tetris(root) <FocusOut> [namespace code {
	    if {[string match %d NotifyAncestor] && \
		    [string match $tetris(start) "Pause"]} {
		if {!$tetris(numplayers)} { Pause }
	    }
	}]
	bind $tetris(root) <FocusIn>  [namespace code {
	    if {[string match %d NotifyAncestor] && \
		    [string match $tetris(start) "Resume"]} {
		if {$tetris(numplayers)} { Resume }
	    }
	}]
    } else {
	foreach i {Unmap Map FocusOut FocusIn} { bind $tetris(root) <$i> {} }
    }
}

proc Pause {} {
    variable tetris
    variable keys

    set tetris(break) 1
    foreach i [after info] { after cancel $i }
    set tetris(start) "Resume"
    bind $tetris(root) <<Left>>		{}
    bind $tetris(root) <<Right>>	{}
    bind $tetris(root) <<RotateLeft>>	{}
    bind $tetris(root) <<RotateRight>>	{}
    bind $tetris(root) <<Slide>>	{}
    bind $tetris(root) <<Drop>>		{}
}

proc Resume {} {
    variable tetris
    variable keys

    set tetris(break) 0
    set tetris(start) "Pause"
    bind $tetris(root) <<Left>>		[namespace code Left]
    bind $tetris(root) <<Right>>	[namespace code Right]
    bind $tetris(root) <<RotateLeft>>	[namespace code {Rotate Left}]
    bind $tetris(root) <<RotateRight>>	[namespace code {Rotate Right}]
    bind $tetris(root) <<Slide>>	[namespace code Slide]
    bind $tetris(root) <<Drop>>		[namespace code Drop]
    GrowRows
    Fall
}

proc GameOver {} {
    variable tetris
    variable widget

    if {$tetris(numplayers)} {
	Reset
    } else {
	foreach i [after info] { after cancel $i }
	set tetris(break) 1
	set tetris(start) "Game Over"
	$widget(board) delete piece
    }
}

proc Reset {} {
    variable tetris
    variable block
    variable stats
    variable widget
    variable color

    Pause
    array set tetris [list start "Start" level $tetris(initLevel) \
	    rows 0 next [random 7] maxbrick 0]
    SetIntervalLevel $tetris(level)
    $widget(board) delete piece struc
    $widget(board) itemconfig back -fill $color([expr {$tetris(level)%7}])
    $widget(next) delete all
    $widget(shadow) dtag shadow
    $widget(shadow) itemconfig all -fill gray
    for {set i -30} {$i < 300} {incr i} { set block($i) 0 }
    for {}          {$i < 310} {incr i} { set block($i) 1 }
    for {set i 0}   {$i < 7}   {incr i} { set stats($i) 0 }
    if {$tetris(numplayers)} {
	incr tetris(deaths)
	Resume
	TellPlayers update 0 $tetris(maxbrick) $tetris(deaths)
    } else {
	set tetris(score) 0
    }
}

proc Options {} {
    variable tetris
    variable widget

    ## These optional windows are already displayed when this is a tclet
    if {$tetris(WWW)} { return }

    set w $widget(opts)
    if {![winfo exists $w]} {
	toplevel $w
	wm withdraw $w
	wm title $w "Tetris v$tetris(version) Options"

	button $w.stats -text "Stats" -command [namespace code Stats]
	button $w.keys -text "Keys Bindings" -command [namespace code Keys]
	button $w.multi -text "Multi Player" -state disabled \
		-command [namespace code MultiPlayer]
	button $w.games -text "Game Types" -command [namespace code GameTypes]
	button $w.about -text "About" -command [namespace code About]
	frame $w.sep -height 2 -bd 2 -relief ridge

	button $w.dis -text "Dismiss" -command [list wm withdraw $w]

	grid $w.stats -sticky ew -padx 2 -pady 2
	grid $w.keys  -sticky ew -padx 2 -pady 2
	grid $w.multi -sticky ew -padx 2 -pady 2
	grid $w.games -sticky ew -padx 2 -pady 2
	grid $w.about -sticky ew -padx 2 -pady 2
	grid $w.sep -sticky ew
	grid $w.dis -sticky ew -padx 4 -pady 4
	grid columnconfig $w 0 -weight 1

	wm resizable $w 1 0
	update idletasks
	set a $tetris(root)
	wm transient $w $a
	wm group $w $a
	wm geometry $w +[expr {[winfo rootx $a]+([winfo width $a]\
		-[winfo reqwidth $w])/2}]+[expr {[winfo rooty $a]\
		+([winfo height $a]-[winfo reqheight $w])/2}]
    }
    ## Only allow multiplayer mode if it was specified and
    ## we succeeded in getting a port
    if {$tetris(multi)} {
	$w.multi configure -state normal
    }
    if {[string compare normal [wm state $w]]} {
	wm deiconify $w
    } else {
	raise $w
    }
}

proc SetKey {key var} {
    variable keys
    variable tetris

    regsub { } $var {} event
    set newevent <Key-$key>

    if {[catch {event add <<$event>> $newevent} err]} {
	bgerror $err
    } else {
	event delete <<$event>> $keys($var)
	set keys($var) $newevent
    }
    if {$tetris(WWW)} {
	focus $tetris(root)
    }
}

proc Keys {} {
    variable tetris
    variable widget
    variable keys

    set w $widget(keys)
    if {![winfo exists $w]} {
	if {$tetris(WWW)} {
	    grid [frame $w] - - - - -sticky new
	} else {
	    toplevel $w
	    wm withdraw $w
	    wm title $w "Tetris v$tetris(version) Keys"
	}
	label $w.l -justify center \
		-text "Key Bindings: Click in widget and hit a key to change"

	label $w.ml -text "Move Left:" -anchor e
	label $w.mr -text "Move Right:" -anchor e
	label $w.rl -text "Rotate Left:" -anchor e
	label $w.rr -text "Rotate Right:" -anchor e
	label $w.sl -text "Slide:" -anchor e
	label $w.dr -text "Drop:" -anchor e

	entry $w.eml -textvariable $tetris(name)keys(Left)
	entry $w.emr -textvariable $tetris(name)keys(Right)
	entry $w.erl -textvariable "$tetris(name)keys(Rotate Left)"
	entry $w.err -textvariable "$tetris(name)keys(Rotate Right)"
	entry $w.esl -textvariable $tetris(name)keys(Slide)
	entry $w.edr -textvariable $tetris(name)keys(Drop)

	bind $w.eml <Any-Key> [namespace code {SetKey %K Left; break}]
	bind $w.emr <Any-Key> [namespace code {SetKey %K Right; break}]
	bind $w.erl <Any-Key> [namespace code {SetKey %K "Rotate Left"; break}]
	bind $w.err <Any-Key> [namespace code {SetKey %K "Rotate Right"; break}]
	bind $w.esl <Any-Key> [namespace code {SetKey %K Slide; break}]
	bind $w.edr <Any-Key> [namespace code {SetKey %K Drop; break}]

	grid $w.l - - - -sticky ew
	grid $w.ml $w.eml $w.rl $w.erl -sticky ew
	grid $w.mr $w.emr $w.rr $w.err -sticky ew
	grid $w.sl $w.esl $w.dr $w.edr -sticky ew

	if {!$tetris(WWW)} {
	    foreach i {AddRow Faster Start Reset Options Quit} {
		set n [string tolower [string index $i 0]]
		label $w.$n -text "$i:" -anchor e
		entry $w.e$n -textvariable $tetris(name)keys($i)
		bind $w.e$n <Any-Key> [namespace code "SetKey %K $i; break"]
	    }

	    grid $w.a $w.ea $w.f $w.ef -sticky ew
	    grid $w.s $w.es $w.r $w.er -sticky ew
	    grid $w.o $w.eo $w.q $w.eq -sticky ew

	    frame $w.sep -height 2 -bd 2 -relief ridge
	    button $w.dis -text "Dismiss" -command [list wm withdraw $w]
	    grid $w.sep - - - -sticky ew
	    grid $w.dis - - - -sticky ew -padx 4 -pady 4
	    wm resizable $w 0 0
	    update idletasks
	    set a $tetris(root)
	    wm transient $w $a
	    wm group $w $a
	    wm geometry $w +[expr {[winfo rootx $a]+([winfo width $a]\
		    -[winfo reqwidth $w])/2}]+[expr {[winfo rooty $a]\
		    +([winfo height $a]-[winfo reqheight $w])/2}]
	}
    }
    if {!$tetris(WWW)} {
	if {[string compare normal [wm state $w]]} {
	    wm deiconify $w
	} else {
	    wm withdraw $w
	}
    }
}

proc GameTypes {} {
    variable tetris
    variable widget

    if {$tetris(WWW)} {
	## Use of pre-defined game types is only for
	## non-plugin users.  Takes up too much screen space
	return
    }
    set w $widget(games)
    if {![winfo exists $w]} {
	toplevel $w
	wm withdraw $w
	wm title $w "Tetris v$tetris(version) Game Types"

	label $w.l -justify center \
		-text "Game Types: Can you master them all?"
	frame $w.left

	# the right side is the info box
	text $w.info -width 24 -height 4 -wrap word \
		-yscrollcommand [list $w.sy set]
	scrollbar $w.sy -orient v -takefocus 0 -bd 1 \
		-command [list $w.info yview]

	$w.info insert end \
		"The following games provide different challenges " {} \
		"from the traditional Tetris environment.  " {} \
		"The point, as always, is to play until the " {} \
		"blocks reach the top, but these pre-set " {} \
		"variations offer further (faster?) ways in which " {} \
		"you can meet your doom!"

	grid $w.l - -sticky ew
	grid $w.left $w.info $w.sy -sticky news
	grid rowconfigure $w 1 -weight 1
	grid columnconfigure $w 1 -weight 1

	button $w.b0 -text "Standard Tetris" -command [namespace code \
		{SetGame 0 1 1 0 0 $tetris(growMax) 0}]
	button $w.b1 -text "I'm on Speeeeed" -command [namespace code \
		{SetGame 15 1 0 0 0 $tetris(growMax) 0}]
	button $w.b2 -text "Row, Row, Row your boat" -command [namespace code \
		{SetGame 10 1 1 1 0 10 21}]
	button $w.b3 -text "Row faster ..." -command [namespace code \
		{SetGame 10 1 1 1 10 20 11}]
	button $w.b4 -text "I'm the Pinball Wizard" -command [namespace code \
		{SetGame 10 0 0 1 15 20 15}]

	grid $w.b0 -in $w.left -stick ew
	grid $w.b1 -in $w.left -stick ew
	grid $w.b2 -in $w.left -stick ew
	grid $w.b3 -in $w.left -stick ew
	grid $w.b4 -in $w.left -stick ew

	frame $w.sep -height 2 -bd 2 -relief ridge
	button $w.b -text "Dismiss" -command [list wm withdraw $w]
	grid $w.sep - -sticky ew
	grid $w.b - -sticky ew -padx 4 -pady 4

	update idletasks
	set a $tetris(root)
	wm transient $w $a
	wm group $w $a
	wm geometry $w +[expr {[winfo rootx $a]+([winfo width $a]\
		-[winfo reqwidth $w])/2}]+[expr {[winfo rooty $a]\
		+([winfo height $a]-[winfo reqheight $w])/2}]
    }
    if {[string compare normal [wm state $w]]} {
	wm deiconify $w
    } else {
	wm withdraw $w
    }
}

proc SetGame {level showNext shadow growing growLevel growMax addRows} {
    variable tetris

    Reset
    foreach var {level showNext shadow growing growMax growLevel} {
	set tetris($var) [set $var]
    }
    while {$addRows > 4} {
	AddRows 4
	# a little anim effect
	update idletasks
	incr addRows -4
    }
    AddRows $addRows
    SetIntervalLevel $tetris(level)
}

proc Stats {} {
    variable tetris
    variable widget
    variable pmap
    variable stats
    variable color

    set w $widget(stats)
    if {![winfo exists $w]} {
	if {$tetris(WWW)} {
	    grid [frame $w] -sticky new -row 0 \
		    -column [lindex [grid size [winfo parent $w]] 0]
	} else {
	    toplevel $w
	    wm withdraw $w
	    wm title $w "Tetris v$tetris(version) Stats"
	}
	set bs $tetris(blocksize)
	grid [label $w.l -text "Piece Statistics"] -sticky ew
	grid [canvas $w.c -bd 2 -width [expr {$bs*9.5}] \
		-height [expr {$bs*22.5}]] -sticky news
	label $w.c.s -text "Session"
	label $w.c.g -text "Game"
	$w.c create window 5 5 -window $w.c.s -anchor nw
	$w.c create window [expr {7*$bs}] 5 \
		-window $w.c.g -anchor nw
	for {set i 0} {$i < 7} {incr i} {
	    foreach p $pmap($i) {
		$w.c create rectangle [lindex $p 0] [lindex $p 1] \
			[lindex $p 2] [lindex $p 3] -tags "p$i piece"
	    }
	    # Oh my gosh, a cheat!
	    $w.c bind piece <<Cheater>> [namespace code {Cheat %W}]
	    foreach {x0 y0 x y} [$w.c bbox p$i] {set x [expr {int($x-$x0)}]}
	    set y [expr {(3*$i+2)*$bs}]
	    $w.c move p$i [expr {int(($bs*9.5-$x)/2-$x0)}] $y
	    $w.c itemconfig p$i -fill $color($i)
	    label $w.c.g$i -textvariable $tetris(name)stats(g$i) -anchor w
	    $w.c create window [expr {.75*$bs}] $y \
		    -window $w.c.g$i -anchor nw
	    label $w.c.$i -textvariable $tetris(name)stats($i) -anchor w
	    $w.c create window [expr {7.5*$bs}] $y \
		    -window $w.c.$i -anchor nw
	}
	if {$tetris(WWW)} {
	    grid [label $w.about -font fixed -text $tetris(info)] -sticky news
	} else {
	    button $w.b -text "Dismiss" -command [list wm withdraw $w]
	    grid $w.b -sticky ew -padx 4 -pady 4
	    wm resizable $w 0 0
	    update idletasks
	    set a $tetris(root)
	    wm transient $w $a
	    wm group $w $a
	    wm geometry $w +[expr {[winfo rootx $a]+([winfo width $a]\
		    -[winfo reqwidth $w])/2}]+[expr {[winfo rooty $a]\
		    +([winfo height $a]-[winfo reqheight $w])/2}]
	}
    }
    if {!$tetris(WWW)} {
	if {[string compare normal [wm state $w]]} {
	    wm deiconify $w
	} else {
	    wm withdraw $w
	}
    }
}

proc InitPieces size {
    variable stats
    variable pmap

    ## Block
    set pmap(0) "{[expr {4*$size}] 0 [expr {5*$size}] $size 4} \
	    {[expr {5*$size}] 0 [expr {6*$size}] $size 5} \
	    {[expr {4*$size}] $size [expr {5*$size}] [expr {2*$size}] 14} \
	    {[expr {5*$size}] $size [expr {6*$size}] [expr {2*$size}] 15}"
    ## L
    set pmap(1) "{[expr {3*$size}] 0 [expr {4*$size}] $size 3} \
	    {[expr {4*$size}] 0 [expr {5*$size}] $size 4} \
	    {[expr {5*$size}] 0 [expr {6*$size}] $size 5} \
	    {[expr {5*$size}] $size [expr {6*$size}] [expr {2*$size}] 15}"
    ## Mirror L
    set pmap(2) "{[expr {3*$size}] 0 [expr {4*$size}] $size 3} \
	    {[expr {4*$size}] 0 [expr {5*$size}] $size 4} \
	    {[expr {5*$size}] 0 [expr {6*$size}] $size 5} \
	    {[expr {3*$size}] $size [expr {4*$size}] [expr {2*$size}] 13}"
    ## Shift One
    set pmap(3) "{[expr {4*$size}] 0 [expr {5*$size}] $size 4} \
	    {[expr {5*$size}] 0 [expr {6*$size}] $size 5} \
	    {[expr {5*$size}] $size [expr {6*$size}] [expr {2*$size}] 15} \
	    {[expr {6*$size}] $size [expr {7*$size}] [expr {2*$size}] 16}"
    ## Shift Two
    set pmap(4) "{[expr {5*$size}] 0 [expr {6*$size}] $size 5} \
	    {[expr {6*$size}] 0 [expr {7*$size}] $size 6} \
	    {[expr {4*$size}] $size [expr {5*$size}] [expr {2*$size}] 14} \
	    {[expr {5*$size}] $size [expr {6*$size}] [expr {2*$size}] 15}"
    ## Bar
    set pmap(5) "{[expr {3*$size}] 0 [expr {4*$size}] $size 3} \
	    {[expr {4*$size}] 0 [expr {5*$size}] $size 4} \
	    {[expr {5*$size}] 0 [expr {6*$size}] $size 5} \
	    {[expr {6*$size}] 0 [expr {7*$size}] $size 6}"
    ## T
    set pmap(6) "{[expr {4*$size}] 0 [expr {5*$size}] $size 4} \
	    {[expr {5*$size}] 0 [expr {6*$size}] $size 5} \
	    {[expr {6*$size}] 0 [expr {7*$size}] $size 6} \
	    {[expr {5*$size}] $size [expr {6*$size}] [expr {2*$size}] 15}"

    for {set i 0} {$i < 7} {incr i} { array set stats "$i 0 g$i 0" }
}

proc ShowNext {} {
    variable tetris
    variable widget
    variable color

    $widget(next) delete all
    if {$tetris(showNext) && [string compare $tetris(start) "Start"]} {
	variable pmap
	foreach i $pmap($tetris(next)) {
	    $widget(next) create rectangle [lindex $i 0] [lindex $i 1] \
		    [lindex $i 2] [lindex $i 3]
	}
	# make sure it is centered
	foreach {x0 y0 x y} [$widget(next) bbox all] {
	    set x [expr {$x-$x0}]
	    set y [expr {$y-$y0}]
	}
	$widget(next) move all \
		[expr {int(([winfo width $widget(next)]-$x)/2-$x0)}] \
		[expr {int(([winfo height $widget(next)]-$y)/2-$y0)}]
	$widget(next) itemconfig all -fill $color($tetris(next))
    }
}

proc CreatePiece {} {
    variable tetris
    variable widget
    variable piece
    variable pmap
    variable stats
    variable block
    variable color

    if {$tetris(growRows)} {
	AddRows $tetris(growRows)
	set tetris(growRows) 0
    }
    set p $tetris(next)
    set j 0
    foreach i $pmap($p) {
	if {$block([set piece($j) [lindex $i 4]])} {
	    GameOver
	    return
	}
	set piece(_$j) [$widget(board) create rectangle \
		[lindex $i 0] [lindex $i 1] [lindex $i 2] [lindex $i 3] \
		-tags "p$p piece"]
	incr j
    }
    incr stats($p)
    incr stats(g$p)
    $widget(board) itemconfig p$p -fill $color($p)
    set tetris(next) [random 7]

    ShowNext
    Shadow
}

proc Cheat {w} {
    if {[regexp {p([0-9])} [$w gettags current] junk i]} {
	variable tetris
	set tetris(next) $i
	ShowNext
    }
}

proc Fall {{a {}}} {
    variable tetris

    if {!$tetris(break)} {
	after $tetris(interval) [namespace code Fall]
	Slide
    }
}

proc Shadow {} {
    variable tetris
    variable widget
    variable piece

    $widget(shadow) dtag shadow
    $widget(shadow) itemconfig all -fill gray
    if {$tetris(shadow) && [string compare {} [$widget(board) bbox piece]]} {
	$widget(shadow) addtag shadow with [expr {$piece(0)%10+1}]
	$widget(shadow) addtag shadow with [expr {$piece(1)%10+1}]
	$widget(shadow) addtag shadow with [expr {$piece(2)%10+1}]
	$widget(shadow) addtag shadow with [expr {$piece(3)%10+1}]
	$widget(shadow) itemconfig shadow -fill black
    }
}

proc GrowRows {{now 0}} {
    variable tetris

    if {$tetris(growing) && !$tetris(break)} {
	if {$now && ($tetris(maxbrick) < $tetris(growMax))} {
	    incr tetris(growRows)
	}
	after $tetris(growInterval) [namespace code {GrowRows 1}]
    }
}

proc CementPiece {} {
    variable tetris
    variable widget
    variable piece
    variable block

    foreach i {0 1 2 3} {
	set block($piece($i)) 1
	set row [expr {$piece($i)/10}]
	$widget(board) addtag row$row with $piece(_$i)
	if {(30-$row)>$tetris(maxbrick)} {
	    set tetris(maxbrick) [expr {30-$row}]
	}
    }
    $widget(board) addtag struc with piece
    $widget(board) itemconfig struc -stipple gray50
    $widget(board) dtag piece
    incr tetris(score) 5
    DropRows
}

proc Slide {} {
    variable tetris
    variable piece
    variable block
    variable widget

    if {[string match [set ix [$widget(board) bbox piece]] {}]} {
	CreatePiece
    } else {
	if {
	    $block([expr {$piece(0)+10}]) || $block([expr {$piece(1)+10}]) ||
	    $block([expr {$piece(2)+10}]) || $block([expr {$piece(3)+10}])
	} {
	    CementPiece
	    update idletasks
	} else {
	    incr piece(0) 10
	    incr piece(1) 10
	    incr piece(2) 10
	    incr piece(3) 10
	    $widget(board) move piece 0 $tetris(blocksize)
	}
    }
}

proc Drop {} {
    variable tetris
    variable piece
    variable block
    variable widget

    set tetris(sync) 1
    if {[string match [set ix [$widget(board) bbox piece]] {}]} return
    set move 0
    while {1} {
	if {
	    $block([expr {$piece(0)+10}]) || $block([expr {$piece(1)+10}]) ||
	    $block([expr {$piece(2)+10}]) || $block([expr {$piece(3)+10}])
	} {
	    break
	} else {
	    incr piece(0) 10
	    incr piece(1) 10
	    incr piece(2) 10
	    incr piece(3) 10
	    incr move $tetris(blocksize)
	}
    }
    $widget(board) move piece 0 $move
    CementPiece
    set tetris(sync) 0
}

proc Left {} {
    variable tetris
    variable piece
    variable block
    variable widget

    if {[string match {} [set ix [$widget(board) bbox piece]]] || \
	    [lindex $ix 0] <= 0} return
    if {
	$block([expr {$piece(0)-1}]) || $block([expr {$piece(1)-1}]) ||
	$block([expr {$piece(2)-1}]) || $block([expr {$piece(3)-1}])
    } {
	return
    } else {
	incr piece(0) -1
	incr piece(1) -1
	incr piece(2) -1
	incr piece(3) -1
	$widget(board) move piece -$tetris(blocksize) 0
	Shadow
	update idletasks
    }
}

proc Right {} {
    variable tetris
    variable piece
    variable block
    variable widget

    if {[string match {} [set ix [$widget(board) bbox piece]]] || \
	    [lindex $ix 2] >= $tetris(width)} return
    if {
	$block([expr {$piece(0)+1}]) || $block([expr {$piece(1)+1}]) ||
	$block([expr {$piece(2)+1}]) || $block([expr {$piece(3)+1}])
    } {
	return
    } else {
	incr piece(0)
	incr piece(1)
	incr piece(2)
	incr piece(3)
	$widget(board) move piece $tetris(blocksize) 0
	Shadow
	update idletasks
    }
}

proc Rotate dir {
    variable tetris
    variable piece
    variable block
    variable widget
    variable coords

    if {[string match {} [set ix [$widget(board) find with piece]]]} return
    foreach {x0 y0 xn yn} [$widget(board) bbox piece] {
	set x [$widget(board) canvasx [expr {($xn+$x0)/2}] $tetris(blocksize)]
	set y [$widget(board) canvasy [expr {($yn+$y0)/2}] $tetris(blocksize)]
    }
    set flag 1
    foreach i $ix {
	set p [$widget(board) coords $i]
	if {[string compare Left $dir]} {
	    set cd "[expr {-[lindex $p 1]+$x+$y}]\
		    [expr { [lindex $p 0]-$x+$y}]\
		    [expr {-[lindex $p 3]+$x+$y}]\
		    [expr { [lindex $p 2]-$x+$y}]"
	} else {
	    set cd "[expr { [lindex $p 1]+$x-$y}]\
		    [expr {-[lindex $p 0]+$x+$y}]\
		    [expr { [lindex $p 3]+$x-$y}]\
		    [expr {-[lindex $p 2]+$x+$y}]"
	}
	if {[string match {} \
		[set n [eval $widget(board) find enclosed $cd]]] || \
		$block([incr n -1])} {
	    set flag 0
	    break
	}
	set m [eval $widget(board) find enclosed $p]
	incr m -1
	array set coords "$m {$i $cd} _$m $n"
    }
    if {$flag} {
	foreach i {0 1 2 3} {
	    eval $widget(board) coords $coords($piece($i))
	    set piece($i) $coords(_$piece($i))
	}
	Shadow
	update idletasks
    }
}

proc DropRows {} {
    variable tetris
    variable block
    variable piece
    variable widget
    variable color

    set full {}
    foreach {i j} [array get piece {[0-3]}] {
	if {[set j [expr {$j/10}]]} { set tmp($j) {} }
    }
    foreach i [array names tmp] {
	if {
	    $block(${i}0) && $block(${i}1) && $block(${i}2) && $block(${i}3) &&
	    $block(${i}4) && $block(${i}5) && $block(${i}6) && $block(${i}7) &&
	    $block(${i}8) && $block(${i}9)
	} {
	    lappend full $i
	    array set block "${i}0 0 ${i}1 0 ${i}2 0 ${i}3 0 \
		    ${i}4 0 ${i}5 0 ${i}6 0 ${i}7 0 ${i}8 0 ${i}9 0"
	}
    }
    if {[set i [llength $full]]} {
	incr tetris(score) [expr {round(pow($i,2))*($tetris(level)+1)}]
	incr tetris(rows)  $i
	incr tetris(maxbrick) -$i
	if {($tetris(rows)/10) > $tetris(level)} {
	    ## Move to the next level
	    incr tetris(level)
	    $widget(board) itemconfig back \
		    -fill $color([expr {$tetris(level)%7}])
	    bell
	    SetIntervalLevel $tetris(level)
	}
	TellPlayers update [incr i -1] $tetris(maxbrick) $tetris(deaths)

	foreach row [lsort -integer $full] {
	    $widget(board) delete row$row
	    for {set i $row; incr i -1} {$i > 0} {incr i -1} {
		$widget(board) move row$i 0 $tetris(blocksize)
		$widget(board) addtag row[expr {$i+1}] with row$i
		$widget(board) dtag row$i
	    }
	    update idletasks
	    for {set i ${row}0} {$i > 0} {incr i -1} {
		if {$block($i)} {
		    set block([expr {$i+10}]) 1
		    set block($i) 0
		}
	    }
	}
	update
    } elseif {$tetris(highband) && $tetris(numplayers)} {
	TellPlayers brick $tetris(maxbrick)
    }
}

proc AddRows {num} {
    variable tetris
    variable block
    variable widget
    variable piece

    if {$num>4 || $num<1} return
    set w $widget(board)

    ## Move pieces
    set bs $tetris(blocksize)

    ## Check if the piece will be moved off the board, if so, we delete it
    set shift [expr {$num*-$bs}]
    $w move piece 0 $shift
    set y [lindex [$w bbox piece] 1]
    if {![string compare {} $y]} {
	# no piece
    } elseif {$y < -2} {
	$w delete piece
    } else {
	set i [expr {$num*-10}]
	incr piece(0) $i
	incr piece(1) $i
	incr piece(2) $i
	incr piece(3) $i
    }

    $w move struc 0 $shift
    for {set i 0} {$i < 31} {incr i} {
	$w addtag row[expr {$i-$num}] with row$i
	$w dtag row$i
    }
    TellPlayers brick [incr tetris(maxbrick) $num]
    if {$tetris(maxbrick)>29} {
	GameOver
	return
    }
    ## Reassign block vars
    set move ${num}0
    for {set i 0} {$i<300} {incr i} {
	if {$block($i)} {
	    set block([expr {$i-$move}]) 1
	    set block($i) 0
	}
    }
    ## Add random black structure blocks
    ## make sure that we don't make a whole row of new blocks
    set numblocks 0
    for {set i [expr {300-$move}]} {$i < 300} {incr i} {
	if {[random] && $numblocks < 9} {
	    set block($i) 1
	    set row [expr {$i/10}]
	    $w create rectangle [expr {($i%10)*$bs}] [expr {$row*$bs}] \
		    [expr {($i%10+1)*$bs}] [expr {($row+1)*$bs}] \
		    -tags "row$row struc" -fill black
	    incr numblocks
	}
	if {($i%10)==0} {
	    set numblocks 0
	}
    }
    $w itemconfig struc -stipple gray50
}

expr {srand([clock clicks]%65536)}
proc random {{range 2}} {
    return [expr {int(rand()*$range)}]
}

Init

}; # end namespace Tetris
