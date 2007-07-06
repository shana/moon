/*
 * runtime.h: Core surface and canvas definitions.
 *
 * Author:
 *   Miguel de Icaza (miguel@novell.com)
 *
 * Copyright 2007 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 * 
 */

#ifndef __RUNTIME_H__
#define __RUNTIME_H__

G_BEGIN_DECLS

#include <stdint.h>
#include <cairo.h>

#include "point.h"
#include "uielement.h"
#include "dependencyobject.h"
#include "value.h"
#include "type.h"
#include "list.h"

#define TIMERS 0
#if TIMERS
#define STARTTIMER(id,str) TimeSpan id##_t_start = get_now(); printf ("timing of '%s' started at %lld\n", str, id##_t_start)
#define ENDTIMER(id,str) TimeSpan id##_t_end = get_now(); printf ("timing of '%s' ended at %lld (%f seconds)\n", str, id##_t_end, (double)(id##_t_end - id##_t_start) / 1000000)
#else
#define STARTTIMER(id,str)
#define ENDTIMER(id,str)
#endif

class Surface {
 public:
	Surface(int width, int height);
	
	~Surface();

	// allows you to redirect painting of the surface to an
	// arbitrary cairo context.
	void Paint (cairo_t *ctx, int x, int y, int width, int height);

	void Attach (UIElement* toplevel);

	void SetCursor (MouseCursor cursor);

	void Resize (int width, int height);
	int GetWidth () { return width; }
	int GetHeight () { return height; }

	void SetTrans (bool trans);
	bool GetTrans () { return transparent; }

	int GetFrameCount () { return frames; }
	void ResetFrameCount () { frames = 0; }

	GtkWidget* GetDrawingArea () { return drawing_area; }
	UIElement* GetToplevel() { return toplevel; }

	void RegisterEvents (callback_mouse_event motion, callback_mouse_event down, callback_mouse_event up,
			     callback_mouse_event enter,
			     callback_plain_event got_focus, callback_plain_event lost_focus,
			     callback_plain_event loaded, callback_plain_event mouse_leave, callback_plain_event surface_resize,
			     callback_keyboard_event keydown, callback_keyboard_event keyup);

	callback_mouse_event cb_motion, cb_down, cb_up, cb_enter;
	callback_plain_event cb_got_focus, cb_lost_focus, cb_loaded, cb_mouse_leave, cb_surface_resize;
	callback_keyboard_event cb_keydown, cb_keyup;

private:
	int width, height;

	// The data lives here
	unsigned char *buffer;

	// The above buffer, as a pixbuf, for the software mode
	GdkPixbuf *pixbuf;
	
	bool using_cairo_xlib_surface;
	
	cairo_surface_t *cairo_buffer_surface;
	cairo_t         *cairo_buffer;
	cairo_t         *cairo_xlib;
	
	//
	// This is what code uses, and its equal to either:
	//    cairo_buffer: when the widget has not been realized
	//    cairo_xlib:   when the widget has been realized
	//
	cairo_t *cairo;		

	// The pixmap used for the backing storage for xlib_surface
	GdkPixmap *pixmap;

	bool transparent;
	
	// The widget where we draw.
	GtkWidget *drawing_area;

	// This currently can only be a canvas.
	UIElement *toplevel;

	// the currently shown cursor
	MouseCursor cursor;

	int frames;

	int last_event_state;
	double last_event_x, last_event_y;

	void ConnectEvents ();
	void Realloc ();

	void CreateSimilarSurface ();

	static void render_cb (gpointer data);
	static void update_input_cb (gpointer data);
	static void drawing_area_size_allocate (GtkWidget *widget, GtkAllocation *allocation, gpointer user_data);
	static void drawing_area_destroyed (GtkWidget *w, gpointer data);
	static gboolean expose_event_callback (GtkWidget *widget, GdkEventExpose *event, gpointer data);
	static gboolean motion_notify_callback (GtkWidget *widget, GdkEventMotion *event, gpointer data);
	static gboolean crossing_notify_callback (GtkWidget *widget, GdkEventCrossing *event, gpointer data);
	static gboolean key_press_callback (GtkWidget *widget, GdkEventKey *key, gpointer data);
	static gboolean key_release_callback (GtkWidget *widget, GdkEventKey *key, gpointer data);
	static gboolean button_release_callback (GtkWidget *widget, GdkEventButton *button, gpointer data);
	static gboolean button_press_callback (GtkWidget *widget, GdkEventButton *button, gpointer data);
	static gboolean realized_callback (GtkWidget *widget, gpointer data);
	static gboolean unrealized_callback (GtkWidget *widget, gpointer data);
};

Surface *surface_new       (int width, int height);
void     surface_resize    (Surface *s, int width, int height);
void     surface_attach    (Surface *s, UIElement *element);
void     surface_init      (Surface *s, int width, int height);
void     surface_destroy   (Surface *s);
void     surface_set_trans (Surface *s, bool trans);
bool     surface_get_trans (Surface *s);
void     surface_paint     (Surface *s, cairo_t *ctx, int x, int y, int width, int height);

void    *surface_get_drawing_area (Surface *s);

void     surface_register_events (Surface *s,
				  callback_mouse_event motion, callback_mouse_event down, callback_mouse_event up,
				  callback_mouse_event enter,
				  callback_plain_event got_focus, callback_plain_event lost_focus,
				  callback_plain_event loaded, callback_plain_event mouse_leave,
				  callback_plain_event surface_resized,
				  callback_keyboard_event keydown, callback_keyboard_event keyup);
		      

cairo_t *measuring_context_create (void);
void     measuring_context_destroy (cairo_t *cr);

void runtime_init (void);

void runtime_shutdown (void);

G_END_DECLS

#endif
