/* -*- Mode: C++; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/*
 * input.cpp: Simulate user input.
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2007-2010 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

#include <stdlib.h>
#include <unistd.h>
#include <math.h>
#include <errno.h>

#define XK_MISCELLANY
#define XK_LATIN1
#include <X11/keysymdef.h>

#include <gdk/gdk.h>

#include "debug.h"
#include "shocker.h"
#include "input.h"

#define XSCREEN_OF_POINTER -1

#define MOVE_MOUSE_LOGARITHMIC_INTERVAL  30000

#define MOUSE_IS_AT_POSITION_TOLERANCE	 2

static void sleep_ms (guint32 ms)
{
	int res;
	timespec tv;
	timespec rem;

	if (ms == 0)
		return;

	rem.tv_sec = ms / 1000;
	rem.tv_nsec = ms * 1000000;

	do {
		tv = rem;
		res = nanosleep (&tv, &rem);
	} while (res == -1 && errno == EINTR);
}

InputProvider *InputProvider::instance = NULL;

InputProvider::InputProvider () : display (NULL), root_window (NULL), xtest_available (false), down_keys (NULL)
{
	display = XOpenDisplay (NULL);
	
	if (!display) {
		printf ("Unable to open XDisplay, input tests will not run.\n");
		return;
	}

	// I guess we'll assume there is only one screen
	root_window = XRootWindow (display, 0);

	if (root_window <= 0) {
		printf ("Unable to get the root window, some of the input tests will not run.\n");
		return;
	}

	int event_base, error_base, majorp, minorp;
	if (!XTestQueryExtension (display, &event_base, &error_base, &majorp, &minorp)) {
		printf ("XTEST Extension unavailable, input tests will not run.\n");
		return;
	}

	xtest_available = true;
	keyboard_speed = 10;

	SendKeyInput (VK_NUMLOCK, true, false, false);

	MoveMouse (0,0);
}

InputProvider::~InputProvider ()
{
	while (down_keys)
		SendKeyInput (GPOINTER_TO_UINT (down_keys->data), false, false, false);

	SendKeyInput (VK_NUMLOCK, false, false, false);
	g_slist_free (down_keys);
}

InputProvider *
InputProvider::GetInstance ()
{
	if (instance == NULL)
		instance = new InputProvider ();
	return instance;
}

void
InputProvider::MoveMouseDirect (int x, int y)
{
	LOG_INPUT ("InputProvider::MoveMouseDirect (%i, %i)\n", x, y);

	g_assert (xtest_available);
	g_assert (display);

	XTestFakeMotionEvent (display, XSCREEN_OF_POINTER, x, y, CurrentTime);
	XFlush (display);
}

void
InputProvider::MoveMouse (int x, int y)
{
	LOG_INPUT ("InputProvider::MoveMouse (%i, %i)\n", x, y);
	
	g_assert (xtest_available);
	g_assert (display);

#if INTERMEDIATE_MOTION
	int current_x;
	int current_y;

	GetCursorPos (current_x, current_y);

	while (current_x != x || current_y != y) {
		XTestFakeMotionEvent (display, XSCREEN_OF_POINTER, 
				      current_x -= (current_x != x ? (current_x > x ? 1 : -1) : 0),
				      current_y -= (current_y != y ? (current_y > y ? 1 : -1) : 0),
				      CurrentTime);
		XFlush (display);
	}
#else
	XTestFakeMotionEvent (display, XSCREEN_OF_POINTER, x, y, CurrentTime);
	XFlush (display);
#endif 
}

void
InputProvider::MoveMouseLogarithmic (int x, int y)
{
	LOG_INPUT ("InputProvider::MoveMouseLogarithmic (%i, %i)\n", x, y);
	
	g_assert (xtest_available);
	g_assert (display);

	int current_x;
	int current_y;
	float mult;
	
	GetCursorPos (current_x, current_y);
	
	while (current_x != x || current_y != y) {
		if (current_x != x) {
			mult = abs (current_x - x) > 20 ? 3.0 : 2.0;
			if (abs (current_x - x) < 10)
				current_x += (current_x < x ? 1.0 : -1.0 );
			else
				current_x += (current_x < x ? 1.0 : -1.0 ) * mult * log (1 + abs (current_x - x));
		}
		
		if (current_y != y) {
			mult = abs (current_y - y) > 20 ? 3.0 : 2.0;
			if (abs (current_y - y) < 10)
				current_y += (current_y < y ? 1.0 : -1.0 );
			else
				current_y += (current_y < y ? 1.0 : -1.0 ) * mult * log (1 + abs (current_y - y));
		}
		
		XTestFakeMotionEvent (display, XSCREEN_OF_POINTER, current_x, current_y, CurrentTime);
		XFlush (display);

		usleep (MOVE_MOUSE_LOGARITHMIC_INTERVAL);
	}
}

bool
InputProvider::MouseIsAtPosition (int x, int y)
{
	LOG_INPUT ("InputProvider::MouseIsAtPosition (%i, %i)\n", x, y);
	int cur_x, cur_y;

	x = MAX (x, 0);
	y = MAX (y, 0);

	GetCursorPos (cur_x, cur_y);

	int delta = MAX (abs (cur_x - x), abs (cur_y - y));
	if (delta <= MOUSE_IS_AT_POSITION_TOLERANCE)
		return true;
	return false;
}

void
InputProvider::MouseDoubleClick (unsigned int delay)
{
	LOG_INPUT ("InputProvider::MouseDoubleClick (%u)\n", delay);
	g_assert (xtest_available);
	g_assert (display);

	XTestFakeButtonEvent (display, 1, true, CurrentTime);
	XFlush (display);
	sleep_ms (delay);

	XTestFakeButtonEvent (display, 1, false, CurrentTime);
	XFlush (display);
	sleep_ms (delay);

	XTestFakeButtonEvent (display, 1, true, CurrentTime);
	XFlush (display);
	sleep_ms (delay);

	XTestFakeButtonEvent (display, 1, false, CurrentTime);
	XFlush (display);
	sleep_ms (delay);
}

void
InputProvider::MouseLeftClick (unsigned int delay)
{
	LOG_INPUT ("InputProvider::MouseLeftClick (%u)\n", delay);
	g_assert (xtest_available);
	g_assert (display);

	XTestFakeButtonEvent (display, 1, true, CurrentTime);
	XFlush (display);
	sleep_ms (delay);

	XTestFakeButtonEvent (display, 1, false, CurrentTime);
	XFlush (display);
	sleep_ms (delay);
}

void
InputProvider::MouseRightClick (unsigned int delay)
{
	LOG_INPUT ("InputProvider::MouseRightClick (%u)\n", delay);
	g_assert (xtest_available);
	g_assert (display);

	XTestFakeButtonEvent (display, 3, true, CurrentTime);
	XFlush (display);
	sleep_ms (delay);

	XTestFakeButtonEvent (display, 3, false, CurrentTime);
	XFlush (display);
	sleep_ms (delay);
}

void
InputProvider::MouseLeftButtonDown (unsigned int delay)
{
	LOG_INPUT ("InputProvider::MouseLeftButtonDown (%u)\n", delay);
	g_assert (xtest_available);
	g_assert (display);

	XTestFakeButtonEvent (display, 1, true, CurrentTime);
	XFlush (display);
	sleep_ms (delay);
}

void
InputProvider::MouseLeftButtonUp (unsigned int delay)
{
	LOG_INPUT ("InputProvider::MouseLeftButtonUp (%u)\n", delay);
	g_assert (xtest_available);
	g_assert (display);

	XTestFakeButtonEvent (display, 1, false, CurrentTime);
	XFlush (display);
	sleep_ms (delay);
}

void
InputProvider::SendKeyInput (guint32 keysym, bool key_down, bool extended, bool unicode)
{
	LOG_INPUT ("InputProvider::SendKeyInput (%i, %i, %i, %i)\n", keysym, key_down, extended, unicode);

	// MS' mac plugin ignores the extended and unicode arguments, so we'll do the same

	g_assert (display);
	g_assert (xtest_available);

	int mapped = MapToKeysym (keysym);
	int keycode = XKeysymToKeycode (display, mapped);

	if (keycode == 0) {
		printf ("Moonlight harness: InputProvider could not map key. keysym: %u, mapped: %i, keycode: %i\n", keysym, mapped, keycode);
		return;
	}

	XTestFakeKeyEvent (display, keycode, key_down, CurrentTime);
	XFlush (display);
	sleep_ms (keyboard_speed - 10);

	if (key_down) {
		if (!g_slist_find (down_keys, GUINT_TO_POINTER (keysym)))
			down_keys = g_slist_append (down_keys, GUINT_TO_POINTER (keysym));
	} else
		down_keys = g_slist_remove (down_keys, GUINT_TO_POINTER (keysym));
}

void
InputProvider::GetCursorPos (int &x, int &y)
{
	LOG_INPUT ("InputProvider::GetCursorPos ()\n");

	g_assert (display);
	g_assert (root_window > 0);

	Window root_return, child_return;
	int x_win, y_win;
	unsigned int mask;

	XQueryPointer (display, root_window, &root_return, &child_return, &x, &y, &x_win, &y_win, &mask);
}

void
InputProvider::MouseWheel (gint16 clicks)
{
	int button;
	
	LOG_INPUT ("InputProvider::MouseWheel (%u)\n", clicks);

	g_assert (display);

	if (clicks == 0)
		return;

	// positive clicks: scroll up, negative clicks: scroll down
	// xlib: button 4: scroll up, button 5: scroll down
	button = clicks > 0 ? 4 : 5;
	
	for (int i = 0; i < abs (clicks); i++) {
		XTestFakeButtonEvent (display, button, true, CurrentTime);
		XTestFakeButtonEvent (display, button, false, CurrentTime);
	}
}

void
InputProvider::SetKeyboardInputSpeed (unsigned int keyboard_input_speed)
{
	LOG_INPUT ("InputProvider::SetKeyboardInputSpeed (%u)\n", keyboard_input_speed);
	keyboard_speed = MIN (10, MAX (1, keyboard_input_speed));
}

int
InputProvider::MapToKeysym (int key)
{
	int res = key;

	switch (key) {

	case VK_CANCEL: res = XK_Cancel; break;
	case VK_BACK: res = XK_BackSpace; break;
	case VK_TAB: res = XK_Tab; break;
	case VK_CLEAR: res = XK_Clear; break;
	case VK_RETURN: res = XK_Return; break;
	case VK_SHIFT: res = XK_Shift_L; break;
	case VK_CONTROL: res = XK_Control_L; break;
	case VK_MENU: res = XK_Alt_L; break;
	case VK_PAUSE: res = XK_Pause; break;
	case VK_CAPITAL: res = XK_Caps_Lock; break;

		/*
	case VK_KANA:
    	case VK_HANGEUL:
    	case VK_HANGUL:
    	case VK_JUNJA:
    	case VK_FINAL:
    	case VK_HANJA:
    	case VK_KANJI:
		*/

	case VK_ESCAPE: res = XK_Escape; break;
//	case VK_CONVERT:	
//	case VK_NONCONVERT:
//	case VK_ACCEPT:

	case VK_MODECHANGE: res = XK_Mode_switch; break;
	case VK_SPACE: res = XK_space; break;
	case VK_PRIOR: res = XK_Prior; break;
	case VK_NEXT: res = XK_Next; break;
	case VK_END: res = XK_End; break;
	case VK_HOME: res = XK_Home; break;
	case VK_LEFT: res = XK_Left; break;
	case VK_UP: res = XK_Up; break;
	case VK_RIGHT: res = XK_Right; break;
	case VK_DOWN: res = XK_Down; break;
	case VK_SELECT: res = XK_Select; break;
	case VK_PRINT: res = XK_Print; break;
	case VK_EXECUTE: res = XK_Execute; break;
	case VK_SNAPSHOT: res = XK_Print; break;
	case VK_INSERT: res = XK_Insert; break;
	case VK_DELETE: res = XK_Delete; break;
	case VK_HELP: res = XK_Help; break;
	case VK_0: res = XK_0; break;
	case VK_1: res = XK_1; break;
	case VK_2: res = XK_2; break;
	case VK_3: res = XK_3; break;
	case VK_4: res = XK_4; break;
	case VK_5: res = XK_5; break;
	case VK_6: res = XK_6; break;
	case VK_7: res = XK_7; break;
	case VK_8: res = XK_8; break;
	case VK_9: res = XK_9; break;
	case VK_A: res = XK_A; break;
	case VK_B: res = XK_B; break;
	case VK_C: res = XK_C; break;
	case VK_D: res = XK_D; break;
	case VK_E: res = XK_E; break;
	case VK_F: res = XK_F; break;
	case VK_G: res = XK_G; break;
	case VK_H: res = XK_H; break;
	case VK_I: res = XK_I; break;
	case VK_J: res = XK_J; break;
	case VK_K: res = XK_K; break;
	case VK_L: res = XK_L; break;
	case VK_M: res = XK_M; break;
	case VK_N: res = XK_N; break;
	case VK_O: res = XK_O; break;
	case VK_P: res = XK_P; break;
	case VK_Q: res = XK_Q; break;
	case VK_R: res = XK_R; break;
	case VK_S: res = XK_S; break;
	case VK_T: res = XK_T; break;
	case VK_U: res = XK_U; break;
	case VK_V: res = XK_V; break;
	case VK_W: res = XK_W; break;
	case VK_X: res = XK_X; break;
	case VK_Y: res = XK_Y; break;
	case VK_Z: res = XK_Z; break;
/*
    case VK_LWIN          = 0x5B,
    case VK_RWIN          = 0x5C,
    case VK_APPS          = 0x5D,

    case VK_SLEEP         = 0x5F,
*/
	case VK_NUMPAD0: res = XK_KP_0; break;
	case VK_NUMPAD1: res = XK_KP_1; break;
	case VK_NUMPAD2: res = XK_KP_2; break;
	case VK_NUMPAD3: res = XK_KP_3; break;
	case VK_NUMPAD4: res = XK_KP_4; break;
	case VK_NUMPAD5: res = XK_KP_5; break;
	case VK_NUMPAD6: res = XK_KP_6; break;
	case VK_NUMPAD7: res = XK_KP_7; break;
	case VK_NUMPAD8: res = XK_KP_8; break;
	case VK_NUMPAD9: res = XK_KP_9; break;
	case VK_MULTIPLY: res = XK_KP_Multiply; break;
	case VK_ADD: case VK_KP_ADD: res = XK_KP_Add; break;
	case VK_SEPARATOR: res = XK_KP_Separator; break;
	case VK_SUBTRACT: res = XK_KP_Subtract; break;
	case VK_DECIMAL: res = XK_KP_Decimal; break;
	case VK_DIVIDE: res = XK_KP_Divide; break;
	case VK_F1: res = XK_F1; break;
	case VK_F2: res = XK_F2; break;
	case VK_F3: res = XK_F3; break;
	case VK_F4: res = XK_F4; break;
	case VK_F5: res = XK_F5; break;
	case VK_F6: res = XK_F6; break;
	case VK_F7: res = XK_F7; break;
	case VK_F8: res = XK_F8; break;
	case VK_F9: res = XK_F9; break;
	case VK_F10: res = XK_F10; break;
	case VK_F11: res = XK_F11; break;
	case VK_F12: res = XK_F12; break;
	case VK_F13: res = XK_F13; break;
	case VK_F14: res = XK_F14; break;
	case VK_F15: res = XK_F15; break;
	case VK_F16: res = XK_F16; break;
	case VK_F17: res = XK_F17; break;
	case VK_F18: res = XK_F18; break;
	case VK_F19: res = XK_F19; break;
	case VK_F20: res = XK_F20; break;
	case VK_F21: res = XK_F21; break;
	case VK_F22: res = XK_F22; break;
	case VK_F23: res = XK_F23; break;
	case VK_F24: res = XK_F24; break;

	case VK_NUMLOCK: res = XK_Num_Lock; break;
	case VK_SCROLL: res = XK_Scroll_Lock;
	case VK_LSHIFT: res = XK_Shift_L; break;
	case VK_RSHIFT: res = XK_Shift_R; break;
	case VK_LCONTROL: res = XK_Control_L; break;
	case VK_RCONTROL: res = XK_Control_R; break;

		/*
	case VK_LMENU:
	case VK_RMENU:
	case VK_OEM_1:
		*/
		
	case VK_OEM_PLUS: res = XK_plus; break;
	case VK_OEM_COMMA: res = XK_comma; break;
	case VK_OEM_MINUS: res = XK_minus; break;
	case VK_OEM_PERIOD: res = XK_period; break;

		/*
	case VK_OEM_2:
	case VK_OEM_3:
		*/

	}

	return res;
}

int InputHelper_MoveMouseLogarithmic (int x, int y)
{
	InputProvider::GetInstance ()->MoveMouseLogarithmic (x, y);
	return 0;
}

int InputHelper_MoveMouseDirect (int x, int y)
{
	InputProvider::GetInstance ()->MoveMouseDirect (x, y);
	return 0;
}

int InputHelper_MouseLeftClick (unsigned int delay)
{
	InputProvider::GetInstance ()->MouseLeftClick (delay);
	return 0;
}

int InputHelper_MouseRightClick (unsigned int delay)
{
	InputProvider::GetInstance ()->MouseRightClick (delay);
	return 0;
}

int InputHelper_MouseDoubleClick (unsigned int delay)
{
	InputProvider::GetInstance ()->MouseDoubleClick (delay);
	return 0;
}

int InputHelper_MouseLeftButtonUp (unsigned int delay)
{
	InputProvider::GetInstance ()->MouseLeftButtonUp (delay);
	return 0;
}

int InputHelper_MouseLeftButtonDown (unsigned int delay)
{
	InputProvider::GetInstance ()->MouseLeftButtonDown (delay);
	return 0;
}

int InputHelper_MouseWheel (gint16 clicks)
{
	InputProvider::GetInstance ()->MouseWheel (clicks);
	return 0;
}

int InputHelper_MouseIsAtPosition (int x, int y, guint8 *result)
{
	*result = InputProvider::GetInstance ()->MouseIsAtPosition (x, y);
	return 0;
}

int InputHelper_SendKeyInput (int vkey, bool down, bool extended, bool unicode)
{
	InputProvider::GetInstance ()->SendKeyInput (vkey, down, extended, unicode);
	return 0;
}

int InputHelper_SetKeyboardInputSpeed (unsigned int keyboardInputSpeed)
{
	InputProvider::GetInstance ()->SetKeyboardInputSpeed (keyboardInputSpeed);
	return 0;
}
