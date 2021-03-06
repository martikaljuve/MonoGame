#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region RESIZABLE_WINDOW Option
// #define RESIZABLE_WINDOW
/* So we've got this silly issue in SDL2's video API at the moment. We can't
 * add/remove the resizable property to the SDL_Window*!
 *
 * So, if you want to have your GameWindow be resizable, uncomment this define.
 * -flibit
 */
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.ComponentModel;

using SDL2;

using Microsoft.Xna.Framework.Graphics;
#endregion

namespace Microsoft.Xna.Framework
{
	class SDL2_GameWindow : GameWindow
	{
		#region Public GameWindow Properties

		[DefaultValue(false)]
		public override bool AllowUserResizing
		{
			/* FIXME: This change should happen immediately. However, SDL2 does
			 * not yet have an SDL_SetWindowResizable, so we mostly just have
			 * this for the #define we've got at the top of this file.
			 * -flibit
			 */
			get
			{
				return (INTERNAL_sdlWindowFlags_Next & SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE) != 0;
			}
			set
			{
				// Note: This can only be used BEFORE window creation!
				if (value)
				{
					INTERNAL_sdlWindowFlags_Next |= SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
				}
				else
				{
					INTERNAL_sdlWindowFlags_Next &= ~SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
				}
			}
		}

		public override Rectangle ClientBounds
		{
			get
			{
				// If we have no client bounds yet, retrieve them
				if (INTERNAL_clientBounds.IsEmpty)
				{
					SDL.SDL_GetWindowPosition(INTERNAL_sdlWindow, out INTERNAL_clientBounds.X, out INTERNAL_clientBounds.Y);
					SDL.SDL_GetWindowSize(INTERNAL_sdlWindow, out INTERNAL_clientBounds.Width, out INTERNAL_clientBounds.Height);
				}

				return INTERNAL_clientBounds;
			}
		}

		public override DisplayOrientation CurrentOrientation
		{
			get
			{
				// SDL2 has no orientation.
				return DisplayOrientation.LandscapeLeft;
			}
		}

		public override IntPtr Handle
		{
			get
			{
				return INTERNAL_sdlWindow;
			}
		}

		public override bool IsBorderless
		{
			get
			{
				return (INTERNAL_sdlWindowFlags_Next & SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS) != 0;
			}
			set
			{
				if (value)
				{
					INTERNAL_sdlWindowFlags_Next |= SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
				}
				else
				{
					INTERNAL_sdlWindowFlags_Next &= ~SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
				}
			}
		}

		public override string ScreenDeviceName
		{
			get
			{
				return INTERNAL_deviceName;
			}
		}

		#endregion

		#region Private SDL2 Window Variables

		private IntPtr INTERNAL_sdlWindow;

		private SDL.SDL_WindowFlags INTERNAL_sdlWindowFlags_Current;
		private SDL.SDL_WindowFlags INTERNAL_sdlWindowFlags_Next;

		private string INTERNAL_deviceName;

		private Rectangle INTERNAL_clientBounds;

		#endregion

		#region Internal Constructor

		internal SDL2_GameWindow()
		{
			INTERNAL_sdlWindowFlags_Next = (
				SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL |
				SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN |
				SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS |
				SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS
			);
#if RESIZABLE_WINDOW
			AllowUserResizing = true;
#else
			AllowUserResizing = false;
#endif

			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_STENCIL_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
#if DEBUG
			SDL.SDL_GL_SetAttribute(
				SDL.SDL_GLattr.SDL_GL_CONTEXT_FLAGS,
				(int) SDL.SDL_GLcontext.SDL_GL_CONTEXT_DEBUG_FLAG
			);
#endif

			string title = MonoGame.Utilities.AssemblyHelper.GetDefaultWindowTitle();
			INTERNAL_sdlWindow = SDL.SDL_CreateWindow(
				title,
				SDL.SDL_WINDOWPOS_CENTERED,
				SDL.SDL_WINDOWPOS_CENTERED,
				GraphicsDeviceManager.DefaultBackBufferWidth,
				GraphicsDeviceManager.DefaultBackBufferHeight,
				INTERNAL_sdlWindowFlags_Next
			);
			INTERNAL_SetIcon(title);

			INTERNAL_sdlWindowFlags_Current = INTERNAL_sdlWindowFlags_Next;
		}

		#endregion

		#region Public GameWindow Methods

		public override void BeginScreenDeviceChange(bool willBeFullScreen)
		{
			// Fullscreen windowflag
			if (willBeFullScreen)
			{
				INTERNAL_sdlWindowFlags_Next |= SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP;
			}
			else
			{
				INTERNAL_sdlWindowFlags_Next &= ~SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP;
			}
		}

		public override void EndScreenDeviceChange(
			string screenDeviceName,
			int clientWidth,
			int clientHeight
		) {
			// Set screen device name, not that we use it...
			INTERNAL_deviceName = screenDeviceName;

			// Fullscreen (Note: this only reads the fullscreen flag)
			SDL.SDL_SetWindowFullscreen(INTERNAL_sdlWindow, (uint) INTERNAL_sdlWindowFlags_Next);

			// Bordered
			SDL.SDL_SetWindowBordered(
				INTERNAL_sdlWindow,
				IsBorderless ? SDL.SDL_bool.SDL_FALSE : SDL.SDL_bool.SDL_TRUE
			);

			/* Because Mac windows resizes from the bottom, we have to get the position before changing
			 * the size so we can keep the window centered when resizing in windowed mode.
			 */
			int prevX = 0;
			int prevY = 0;
			if ((INTERNAL_sdlWindowFlags_Next & SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP) == 0)
			{
				SDL.SDL_GetWindowPosition(INTERNAL_sdlWindow, out prevX, out prevY);
			}

			// Window bounds
			SDL.SDL_SetWindowSize(INTERNAL_sdlWindow, clientWidth, clientHeight);

			// Window position
			if (	(INTERNAL_sdlWindowFlags_Current & SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP) == SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP &&
				(INTERNAL_sdlWindowFlags_Next & SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP) == 0	)
			{
				// If exiting fullscreen, just center the window on the desktop.
				SDL.SDL_SetWindowPosition(
					INTERNAL_sdlWindow,
					SDL.SDL_WINDOWPOS_CENTERED,
					SDL.SDL_WINDOWPOS_CENTERED
				);
			}
			else if ((INTERNAL_sdlWindowFlags_Next & SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP) == 0)
			{
				SDL.SDL_SetWindowPosition(
					INTERNAL_sdlWindow,
					prevX + ((OpenGLDevice.Instance.Backbuffer.Width - clientWidth) / 2),
					prevY + ((OpenGLDevice.Instance.Backbuffer.Height - clientHeight) / 2)
				);
			}

			// Current flags have just been updated.
			INTERNAL_sdlWindowFlags_Current = INTERNAL_sdlWindowFlags_Next;
		}

		#endregion

		#region Internal Methods

		internal void INTERNAL_ClientSizeChanged()
		{
			// Update our client bounds rectangle before invoking the event
			SDL.SDL_GetWindowPosition(INTERNAL_sdlWindow, out INTERNAL_clientBounds.X, out INTERNAL_clientBounds.Y);
			SDL.SDL_GetWindowSize(INTERNAL_sdlWindow, out INTERNAL_clientBounds.Width, out INTERNAL_clientBounds.Height);

			OnClientSizeChanged();
		}

		#endregion

		#region Protected GameWindow Methods

		protected internal override void SetSupportedOrientations(DisplayOrientation orientations)
		{
			// No-op. SDL2 has no orientation.
		}

		protected override void SetTitle(string title)
		{
			SDL.SDL_SetWindowTitle(
				INTERNAL_sdlWindow,
				title
			);
		}

		#endregion

		#region Private Window Icon Method

		private void INTERNAL_SetIcon(string title)
		{
			string fileIn = String.Empty;

			/* If the game's using SDL2_image, provide the option to use a PNG
			 * instead of a BMP. Nice for anyone who cares about transparency.
			 * -flibit
			 */
			try
			{
				fileIn = INTERNAL_GetIconName(title, ".png");
				if (!String.IsNullOrEmpty(fileIn))
				{
					IntPtr icon = SDL_image.IMG_Load(fileIn);
					SDL.SDL_SetWindowIcon(INTERNAL_sdlWindow, icon);
					SDL.SDL_FreeSurface(icon);
					return;
				}
			}
			catch(DllNotFoundException)
			{
				// Not that big a deal guys.
			}

			fileIn = INTERNAL_GetIconName(title, ".bmp");
			if (!String.IsNullOrEmpty(fileIn))
			{
				IntPtr icon = SDL.SDL_LoadBMP(fileIn);
				SDL.SDL_SetWindowIcon(INTERNAL_sdlWindow, icon);
				SDL.SDL_FreeSurface(icon);
			}
		}

		#endregion

		#region Private Static Icon Filename Method

		private static string INTERNAL_GetIconName(string title, string extension)
		{
			string fileIn = String.Empty;
			if (System.IO.File.Exists(title + extension))
			{
				// If the title and filename work, it just works. Fine.
				fileIn = title + extension;
			}
			else
			{
				// But sometimes the title has invalid characters inside.

				/* In addition to the filesystem's invalid charset, we need to
				 * blacklist the Windows standard set too, no matter what.
				 * -flibit
				 */
				char[] hardCodeBadChars = new char[]
				{
					'<',
					'>',
					':',
					'"',
					'/',
					'\\',
					'|',
					'?',
					'*'
				};
				List<char> badChars = new List<char>();
				badChars.AddRange(System.IO.Path.GetInvalidFileNameChars());
				badChars.AddRange(hardCodeBadChars);

				string stripChars = title;
				foreach (char c in badChars)
				{
					stripChars = stripChars.Replace(c.ToString(), "");
				}
				stripChars += extension;

				if (System.IO.File.Exists(stripChars))
				{
					fileIn = stripChars;
				}
			}
			return fileIn;
		}

		#endregion
	}
}
