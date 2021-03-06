﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VideoViewModel.cs" company="HandBrake Project (http://handbrake.fr)">
//   This file is part of the HandBrake source code - It may be used under the terms of the GNU General Public License.
// </copyright>
// <summary>
//   The Video View Model
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HandBrakeWPF.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Windows;

    using Caliburn.Micro;

    using HandBrake.ApplicationServices.Model;
    using HandBrake.ApplicationServices.Model.Encoding;
    using HandBrake.ApplicationServices.Parsing;
    using HandBrake.ApplicationServices.Utilities;
    using HandBrake.Interop;
    using HandBrake.Interop.Model.Encoding;
    using HandBrake.Interop.Model.Encoding.x264;
    using HandBrake.Interop.Model.Encoding.x265;

    using HandBrakeWPF.Commands.Interfaces;
    using HandBrakeWPF.Properties;
    using HandBrakeWPF.Services.Interfaces;
    using HandBrakeWPF.ViewModels.Interfaces;

    /// <summary>
    /// The Video View Model
    /// </summary>
    public class VideoViewModel : ViewModelBase, IVideoViewModel
    {
        #region Constants and Fields
        /// <summary>
        /// Same as source constant.
        /// </summary>
        private const string SameAsSource = "Same as source";

        /// <summary>
        /// The possible h264 levels.
        /// </summary>
        private static readonly List<string> Levels = new List<string> { "Auto", "1.0", "1b", "1.1", "1.2", "1.3", "2.0", "2.1", "2.2", "3.0", "3.1", "3.2", "4.0", "4.1", "4.2", "5.0", "5.1", "5.2" };

        /// <summary>
        /// Backing field for the user setting service.
        /// </summary>
        private readonly IUserSettingService userSettingService;

        /// <summary>
        /// The advanced encoder options command
        /// </summary>
        private readonly IAdvancedEncoderOptionsCommand advancedEncoderOptionsCommand;

        /// <summary>
        /// Backing field used to display / hide the x264 options
        /// </summary>
        private bool displayX264Options;

        /// <summary>
        /// Backing field used to display / hide the x265 options
        /// </summary>
        private bool displayX265Options;

        /// <summary>
        /// The display qsv options.
        /// </summary>
        private bool displayQSVOptions;

        /// <summary>
        /// Backing field used to display / hide the h264 options
        /// </summary>
        private bool displayEncoderOptions;

        /// <summary>
        /// The quality max.
        /// </summary>
        private int qualityMax;

        /// <summary>
        /// The quality min.
        /// </summary>
        private int qualityMin;

        /// <summary>
        /// The show peak framerate.
        /// </summary>
        private bool showPeakFramerate;

        /// <summary>
        /// The show peak framerate.
        /// </summary>
        private int rf;

        /// <summary>
        /// The x264 preset value.
        /// </summary>
        private int x264PresetValue;

        /// <summary>
        /// The qsv preset value.
        /// </summary>
        private int qsvPresetValue;

        /// <summary>
        /// The can clear tracker.
        /// </summary>
        private bool canClear;

        /// <summary>
        /// The use advanced tab.
        /// </summary>
        private bool useAdvancedTab;

        /// <summary>
        /// The display framerate controls.
        /// </summary>
        private bool displayNonQSVControls;

        /// <summary>
        /// The x265 preset value.
        /// </summary>
        private int x265PresetValue;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoViewModel"/> class.
        /// </summary>
        /// <param name="userSettingService">
        /// The user Setting Service.
        /// </param>
        /// <param name="advancedEncoderOptionsCommand">
        /// The advanced Encoder Options Command.
        /// </param>
        public VideoViewModel(IUserSettingService userSettingService, IAdvancedEncoderOptionsCommand advancedEncoderOptionsCommand)
        {
            this.Task = new EncodeTask { VideoEncoder = VideoEncoder.X264 };
            this.userSettingService = userSettingService;
            this.advancedEncoderOptionsCommand = advancedEncoderOptionsCommand;
            this.QualityMin = 0;
            this.QualityMax = 51;
            this.IsConstantQuantity = true;
            this.VideoEncoders = EnumHelper<VideoEncoder>.GetEnumList();

            X264Presets = new BindingList<x264Preset>(EnumHelper<x264Preset>.GetEnumList().ToList());
            QsvPresets = new BindingList<QsvPreset>(EnumHelper<QsvPreset>.GetEnumList().ToList());
            H264Profiles = EnumHelper<x264Profile>.GetEnumList();
            X264Tunes = EnumHelper<x264Tune>.GetEnumList().Where(t => t != x264Tune.Fastdecode);
            this.H264Levels = Levels;

            X265Presets = new BindingList<x265Preset>(EnumHelper<x265Preset>.GetEnumList().ToList());
            H265Profiles = EnumHelper<x265Profile>.GetEnumList();
            X265Tunes = EnumHelper<x265Tune>.GetEnumList();

            this.userSettingService.SettingChanged += this.UserSettingServiceSettingChanged;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the current Encode Task.
        /// </summary>
        public EncodeTask Task { get; set; }

        /// <summary>
        /// Gets a value indicating whether show advanced tab.
        /// </summary>
        public bool ShowAdvancedTab
        {
            get
            {
                bool showAdvTabSetting = this.userSettingService.GetUserSetting<bool>(UserSettingConstants.ShowAdvancedTab);
                if (!showAdvTabSetting)
                {
                    this.UseAdvancedTab = false;
                }

                if (this.SelectedVideoEncoder == VideoEncoder.QuickSync)
                {
                    return false;
                }

                return showAdvTabSetting;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether use video tab.
        /// </summary>
        public bool UseAdvancedTab
        {
            get
            {
                return this.useAdvancedTab;
            }
            set
            {
                if (!object.Equals(value, this.useAdvancedTab))
                {
                    // Set the Advanced Tab up with the current settings, if we can.
                    if (value)
                    {
                        this.Task.AdvancedEncoderOptions = this.GetActualx264Query();
                    }

                    if (!value)
                    {
                        this.Task.AdvancedEncoderOptions = string.Empty;
                    }

                    this.useAdvancedTab = value;
                    this.Task.ShowAdvancedTab = value;
                    this.NotifyOfPropertyChange(() => this.UseAdvancedTab);
                }
            }
        }

        /// <summary>
        /// Gets Framerates.
        /// </summary>
        public IEnumerable<string> Framerates
        {
            get
            {
                return new List<string> { "Same as source", "5", "10", "12", "15", "23.976", "24", "25", "29.97", "30", "50", "59.94", "60" };
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether IsConstantFramerate.
        /// </summary>
        public bool IsConstantFramerate
        {
            get
            {
                return this.Task.FramerateMode == FramerateMode.CFR;
            }
            set
            {
                if (value)
                {
                    this.Task.FramerateMode = FramerateMode.CFR;
                    this.IsVariableFramerate = false;
                    this.IsPeakFramerate = false;
                }

                this.NotifyOfPropertyChange(() => this.IsConstantFramerate);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether IsConstantQuantity.
        /// </summary>
        public bool IsConstantQuantity
        {
            get
            {
                return this.Task.VideoEncodeRateType == VideoEncodeRateType.ConstantQuality;
            }
            set
            {
                if (value)
                {
                    this.Task.VideoEncodeRateType = VideoEncodeRateType.ConstantQuality;
                    this.Task.TwoPass = false;
                    this.Task.TurboFirstPass = false;
                    this.Task.VideoBitrate = null;
                    this.NotifyOfPropertyChange(() => this.Task);
                }
                else
                {
                    this.Task.VideoEncodeRateType = VideoEncodeRateType.AverageBitrate;
                }

                this.NotifyOfPropertyChange(() => this.IsConstantQuantity);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether IsPeakFramerate.
        /// </summary>
        public bool IsPeakFramerate
        {
            get
            {
                return this.Task.FramerateMode == FramerateMode.PFR;
            }
            set
            {
                if (value)
                {
                    this.Task.FramerateMode = FramerateMode.PFR;
                    this.IsVariableFramerate = false;
                    this.IsConstantFramerate = false;
                }

                this.NotifyOfPropertyChange(() => this.IsPeakFramerate);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether IsVariableFramerate.
        /// </summary>
        public bool IsVariableFramerate
        {
            get
            {
                return this.Task.FramerateMode == FramerateMode.VFR;
            }
            set
            {
                if (value)
                {
                    this.IsPeakFramerate = false;
                    this.IsConstantFramerate = false;
                    this.Task.FramerateMode = FramerateMode.VFR;
                }

                this.NotifyOfPropertyChange(() => this.IsVariableFramerate);
            }
        }

        /// <summary>
        /// Gets a value indicating whether is lossless.
        /// </summary>
        public bool IsLossless
        {
            get
            {
                return 0.0.Equals(this.DisplayRF) && this.SelectedVideoEncoder == VideoEncoder.X264;
            }
        }

        /// <summary>
        /// Gets or sets QualityMax.
        /// </summary>
        public int QualityMax
        {
            get
            {
                return this.qualityMax;
            }
            set
            {
                if (!qualityMax.Equals(value))
                {
                    this.qualityMax = value;
                    this.NotifyOfPropertyChange(() => this.QualityMax);
                }
            }
        }

        /// <summary>
        /// Gets or sets QualityMin.
        /// </summary>
        public int QualityMin
        {
            get
            {
                return this.qualityMin;
            }
            set
            {
                if (!qualityMin.Equals(value))
                {
                    this.qualityMin = value;
                    this.NotifyOfPropertyChange(() => this.QualityMin);
                }
            }
        }

        /// <summary>
        /// Gets or sets RF.
        /// </summary>
        public int RF
        {
            get
            {
                return rf;
            }
            set
            {
                this.rf = value;

                this.SetQualitySliderBounds();
                switch (this.SelectedVideoEncoder)
                {
                    case VideoEncoder.FFMpeg:
                    case VideoEncoder.FFMpeg2:
                        this.Task.Quality = (32 - value);
                        break;    
                    case VideoEncoder.VP8:
                        this.Task.Quality = (63 - value);
                        break;
                    case VideoEncoder.X264:
                    case VideoEncoder.X265:
                        double cqStep = userSettingService.GetUserSetting<double>(UserSettingConstants.X264Step);
                        double rfValue = 51.0 - value * cqStep;
                        rfValue = Math.Round(rfValue, 2);
                        this.Task.Quality = rfValue;
                        break;           
                    case VideoEncoder.QuickSync:
                        rfValue = 51.0 - value;
                        rfValue = Math.Round(rfValue, 0);
                        this.Task.Quality = rfValue;
                        break;
                    case VideoEncoder.Theora:                 
                        Task.Quality = value;
                        break;
                }

                this.NotifyOfPropertyChange(() => this.RF);
                this.NotifyOfPropertyChange(() => this.DisplayRF);
                this.NotifyOfPropertyChange(() => this.IsLossless);
            }
        }

        /// <summary>
        /// Gets DisplayRF.
        /// </summary>
        public double DisplayRF
        {
            get
            {
                return Task.Quality.HasValue ? this.Task.Quality.Value : 0;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether two pass.
        /// </summary>
        public bool TwoPass
        {
            get
            {
                return this.Task.TwoPass;
            }

            set
            {
                this.Task.TwoPass = value;
                this.NotifyOfPropertyChange(() => this.TwoPass);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether turbo first pass.
        /// </summary>
        public bool TurboFirstPass
        {
            get
            {
                return this.Task.TurboFirstPass;
            }

            set
            {
                this.Task.TurboFirstPass = value;
                this.NotifyOfPropertyChange(() => this.TurboFirstPass);
            }
        }

        /// <summary>
        /// Gets the rfqp.
        /// </summary>
        public string Rfqp
        {
            get
            {
                return this.SelectedVideoEncoder == VideoEncoder.X264 || this.SelectedVideoEncoder == VideoEncoder.X265 ? "RF" : "QP";
            }
        }

        /// <summary>
        /// Gets the high quality label.
        /// </summary>
        public string HighQualityLabel
        {
            get
            {
                return this.SelectedVideoEncoder == VideoEncoder.X264 ? Resources.Video_PlaceboQuality : Resources.Video_HigherQuality;
            }
        }

        /// <summary>
        /// Gets or sets SelectedFramerate.
        /// </summary>
        public string SelectedFramerate
        {
            get
            {
                if (this.Task.Framerate == null)
                {
                    return "Same as source";
                }

                return this.Task.Framerate.Value.ToString(CultureInfo.InvariantCulture);
            }
            set
            {
                if (value == "Same as source")
                {
                    this.Task.Framerate = null;
                    this.ShowPeakFramerate = false;

                    if (this.Task.FramerateMode == FramerateMode.PFR)
                    {
                        this.IsVariableFramerate = true;
                    }
                }
                else if (!string.IsNullOrEmpty(value))
                {
                    this.ShowPeakFramerate = true;
                    if (this.Task.FramerateMode == FramerateMode.VFR)
                    {
                        this.IsPeakFramerate = true;
                    }
                    this.Task.Framerate = double.Parse(value, CultureInfo.InvariantCulture);
                }

                this.NotifyOfPropertyChange(() => this.SelectedFramerate);
                this.NotifyOfPropertyChange(() => this.Task);
            }
        }

        /// <summary>
        /// Gets or sets SelectedVideoEncoder.
        /// </summary>
        public VideoEncoder SelectedVideoEncoder
        {
            get
            {
                return this.Task.VideoEncoder;
            }
            set
            {
                this.Task.VideoEncoder = value;
                this.NotifyOfPropertyChange(() => this.SelectedVideoEncoder);

                // Tell the Advanced Panel off the change
                IAdvancedViewModel advancedViewModel = IoC.Get<IAdvancedViewModel>();
                advancedViewModel.SetEncoder(this.Task.VideoEncoder);

                // Update the Quality Slider. Make sure the bounds are up to date with the users settings.
                this.SetQualitySliderBounds();

                // Hide the x264 controls when not needed.
                this.DisplayX264Options = value == VideoEncoder.X264;

                // Hide the x265 controls when not needed.
                this.DisplayX265Options = value == VideoEncoder.X265;

                this.DisplayQSVOptions = value == VideoEncoder.QuickSync;
                this.DisplayH264Options = value == VideoEncoder.X264 || value == VideoEncoder.QuickSync;
                this.UseAdvancedTab = value != VideoEncoder.QuickSync && this.UseAdvancedTab;
                this.DisplayNonQSVControls = value != VideoEncoder.QuickSync;

                this.NotifyOfPropertyChange(() => this.Rfqp);
                this.NotifyOfPropertyChange(() => this.ShowAdvancedTab);

                this.NotifyOfPropertyChange(() => this.HighQualityLabel);
                if (value == VideoEncoder.QuickSync)
                {
                    this.IsConstantFramerate = true;
                    this.TwoPass = false;
                    this.TurboFirstPass = false;
                    this.Task.Framerate = null;
                    this.NotifyOfPropertyChange(() => SelectedFramerate);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether ShowPeakFramerate.
        /// </summary>
        public bool ShowPeakFramerate
        {
            get
            {
                return this.showPeakFramerate;
            }
            set
            {
                this.showPeakFramerate = value;
                this.NotifyOfPropertyChange(() => this.ShowPeakFramerate);
            }
        }

        /// <summary>
        /// Gets or sets VideoEncoders.
        /// </summary>
        public IEnumerable<VideoEncoder> VideoEncoders { get; set; }

        /// <summary>
        /// Gets or sets the extra arguments.
        /// </summary>
        public string ExtraArguments
        {
            get
            {
                return this.Task.ExtraAdvancedArguments;
            }
            set
            {
                if (!object.Equals(this.Task.ExtraAdvancedArguments, value))
                {
                    this.Task.ExtraAdvancedArguments = value;
                    this.NotifyOfPropertyChange(() => this.ExtraArguments);
                    this.NotifyOfPropertyChange(() => FullOptionsTooltip);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to display H264
        /// </summary>
        public bool DisplayH264Options
        {
            get
            {
                return this.displayEncoderOptions;
            }
            set
            {
                this.displayEncoderOptions = value;
                this.NotifyOfPropertyChange(() => this.DisplayH264Options);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether display x 264 options.
        /// </summary>
        public bool DisplayX264Options
        {
            get
            {
                return this.displayX264Options;
            }
            set
            {
                this.displayX264Options = value;
                this.NotifyOfPropertyChange(() => this.DisplayX264Options);
                this.NotifyOfPropertyChange(() => FullOptionsTooltip);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether display x 264 options.
        /// </summary>
        public bool DisplayX265Options
        {
            get
            {
                return this.displayX265Options;
            }
            set
            {
                this.displayX265Options = value;
                this.NotifyOfPropertyChange(() => this.DisplayX265Options);
                this.NotifyOfPropertyChange(() => FullOptionsTooltip);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to display qsv options.
        /// </summary>
        public bool DisplayQSVOptions
        {
            get
            {
                return this.displayQSVOptions;
            }
            set
            {
                this.displayQSVOptions = value;
                this.NotifyOfPropertyChange(() => this.DisplayQSVOptions);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether display framerate controls.
        /// </summary>
        public bool DisplayNonQSVControls
        {
            get
            {
                return this.displayNonQSVControls;
            }

            set
            {
                this.displayNonQSVControls = value;
                this.NotifyOfPropertyChange(() => this.DisplayNonQSVControls);
            }
        }

        /// <summary>
        /// Gets or sets the x 264 preset value.
        /// </summary>
        public int X264PresetValue
        {
            get
            {
                return this.x264PresetValue;
            }
            set
            {
                if (!object.Equals(this.X264PresetValue, value))
                {
                    this.x264PresetValue = value;
                    this.X264Preset = this.X264Presets[value];
                    this.NotifyOfPropertyChange(() => this.x264PresetValue);
                    this.NotifyOfPropertyChange(() => FullOptionsTooltip);
                }
            }
        }

        /// <summary>
        /// Gets or sets X264Preset.
        /// </summary>
        public x264Preset X264Preset
        {
            get
            {
                return this.Task.X264Preset;
            }
            set
            {
                if (!object.Equals(this.X264Preset, value))
                {
                    this.Task.X264Preset = value;
                    this.NotifyOfPropertyChange(() => this.X264Preset);
                    ResetAdvancedTab();
                    this.NotifyOfPropertyChange(() => FullOptionsTooltip);
                }
            }
        }

        /// <summary>
        /// Gets or sets X264Preset.
        /// </summary>
        public QsvPreset QsvPreset
        {
            get
            {
                return this.Task.QsvPreset;
            }
            set
            {
                if (!object.Equals(this.QsvPreset, value))
                {
                    this.Task.QsvPreset = value;
                    this.NotifyOfPropertyChange(() => this.QsvPreset);
                }
            }
        }

        /// <summary>
        /// Gets or sets the x 264 preset value.
        /// </summary>
        public int QsvPresetValue
        {
            get
            {
                return this.qsvPresetValue;
            }
            set
            {
                if (!object.Equals(this.QsvPresetValue, value))
                {
                    this.qsvPresetValue = value;
                    this.QsvPreset = this.QsvPresets[value];
                    this.NotifyOfPropertyChange(() => this.QsvPresetValue);
                }
            }
        }

        /// <summary>
        /// Gets or sets H264Profile.
        /// </summary>
        public x264Profile H264Profile
        {
            get
            {
                return this.Task.H264Profile;
            }

            set
            {
                if (!object.Equals(this.H264Profile, value))
                {
                    this.Task.H264Profile = value;
                    this.NotifyOfPropertyChange(() => this.H264Profile);
                    ResetAdvancedTab();
                    this.NotifyOfPropertyChange(() => FullOptionsTooltip);
                }
            }
        }

        /// <summary>
        /// Gets or sets H264Profile.
        /// </summary>
        public string H264Level
        {
            get
            {
                return this.Task.H264Level;
            }
            set
            {
                if (!object.Equals(this.H264Level, value))
                {
                    this.Task.H264Level = value;
                    this.NotifyOfPropertyChange(() => this.H264Level);
                    ResetAdvancedTab();
                    this.NotifyOfPropertyChange(() => FullOptionsTooltip);
                }
            }
        }

        /// <summary>
        /// Gets or sets X264Tune.
        /// </summary>
        public x264Tune X264Tune
        {
            get
            {
                return this.Task.X264Tune;
            }
            set
            {
                if (!object.Equals(this.X264Tune, value))
                {
                    this.Task.X264Tune = value;
                    this.NotifyOfPropertyChange(() => this.X264Tune);
                    ResetAdvancedTab();
                    this.NotifyOfPropertyChange(() => FullOptionsTooltip);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether fast decode.
        /// </summary>
        public bool FastDecode
        {
            get
            {
                return this.Task.FastDecode;
            }
            set
            {
                if (!object.Equals(this.FastDecode, value))
                {
                    this.Task.FastDecode = value;
                    this.NotifyOfPropertyChange(() => this.FastDecode);
                    ResetAdvancedTab();
                    this.NotifyOfPropertyChange(() => FullOptionsTooltip);
                }
            }
        }

        /// <summary>
        /// Gets or sets X264Presets.
        /// </summary>
        public BindingList<x264Preset> X264Presets { get; set; }

        /// <summary>
        /// Gets or sets QsvPreset.
        /// </summary>
        public BindingList<QsvPreset> QsvPresets { get; set; }

        /// <summary>
        /// Gets or sets X264Profiles.
        /// </summary>
        public IEnumerable<x264Profile> H264Profiles { get; set; }

        /// <summary>
        /// Gets or sets X264Tunes.
        /// </summary>
        public IEnumerable<x264Tune> X264Tunes { get; set; }

        /// <summary>
        /// Gets or sets the x 264 levels.
        /// </summary>
        public IEnumerable<string> H264Levels { get; set; }

        /// <summary>
        /// Gets the full options tooltip.
        /// </summary>
        public string FullOptionsTooltip
        {
            get
            {
                return string.Format(Resources.Video_EncoderExtraArgs, this.GetActualx264Query()); // "You can provide additional arguments using the standard x264 format"; 
            }
        }

        /// <summary>
        /// Gets the qsv slider max.
        /// </summary>
        public int QsvSliderMax
        {
            get
            {
                return SystemInfo.IsHswOrNewer ? 2 : 1;
            }
        }

        /// <summary>
        /// Gets or sets the x 265 preset value.
        /// </summary>
        public int X265PresetValue
        {
            get
            {
                return this.x265PresetValue;
            }
            set
            {
                if (!object.Equals(this.X265PresetValue, value))
                {
                    this.x265PresetValue = value;
                    this.X265Preset = this.X265Presets[value];
                    this.NotifyOfPropertyChange(() => this.x265PresetValue);
                    this.NotifyOfPropertyChange(() => FullOptionsTooltip);
                }
            }
        }

        /// <summary>
        /// Gets or sets X265Preset.
        /// </summary>
        public x265Preset X265Preset
        {
            get
            {
                return this.Task.X265Preset;
            }
            set
            {
                if (!object.Equals(this.X265Preset, value))
                {
                    this.Task.X265Preset = value;
                    this.NotifyOfPropertyChange(() => this.X265Preset);
                    ResetAdvancedTab();
                    this.NotifyOfPropertyChange(() => FullOptionsTooltip);
                }
            }
        }

        /// <summary>
        /// Gets or sets H265Profile.
        /// </summary>
        public x265Profile H265Profile
        {
            get
            {
                return this.Task.H265Profile;
            }

            set
            {
                if (!object.Equals(this.H265Profile, value))
                {
                    this.Task.H265Profile = value;
                    this.NotifyOfPropertyChange(() => this.H265Profile);
                    ResetAdvancedTab();
                    this.NotifyOfPropertyChange(() => FullOptionsTooltip);
                }
            }
        }

        /// <summary>
        /// Gets or sets X265Tune.
        /// </summary>
        public x265Tune X265Tune
        {
            get
            {
                return this.Task.X265Tune;
            }
            set
            {
                if (!object.Equals(this.X265Tune, value))
                {
                    this.Task.X265Tune = value;
                    this.NotifyOfPropertyChange(() => this.X265Tune);
                    ResetAdvancedTab();
                    this.NotifyOfPropertyChange(() => FullOptionsTooltip);
                }
            }
        }

        /// <summary>
        /// Gets or sets X265Presets.
        /// </summary>
        public BindingList<x265Preset> X265Presets { get; set; }

        /// <summary>
        /// Gets or sets X265Profiles.
        /// </summary>
        public IEnumerable<x265Profile> H265Profiles { get; set; }

        /// <summary>
        /// Gets or sets X265Tunes.
        /// </summary>
        public IEnumerable<x265Tune> X265Tunes { get; set; }
        #endregion

        #region Public Methods

        /// <summary>
        /// Setup this window for a new source
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="preset">
        /// The preset.
        /// </param>
        /// <param name="task">
        /// The task.
        /// </param>
        public void SetSource(Title title, Preset preset, EncodeTask task)
        {
            this.Task = task;
        }

        /// <summary>
        /// Setup this tab for the specified preset.
        /// </summary>
        /// <param name="preset">
        /// The preset.
        /// </param>
        /// <param name="task">
        /// The task.
        /// </param>
        public void SetPreset(Preset preset, EncodeTask task)
        {
            this.Task = task;
            if (preset == null || preset.Task == null)
            {
                return;
            }

            this.SelectedVideoEncoder = preset.Task.VideoEncoder;
            this.SelectedFramerate = preset.Task.Framerate.HasValue ? preset.Task.Framerate.Value.ToString(CultureInfo.InvariantCulture) : SameAsSource;

            this.IsConstantQuantity = preset.Task.VideoEncodeRateType == VideoEncodeRateType.ConstantQuality;

            switch (preset.Task.FramerateMode)
            {
                case FramerateMode.CFR:
                    this.IsConstantFramerate = true;
                    break;
                case FramerateMode.VFR:
                    this.IsVariableFramerate = true;
                    this.ShowPeakFramerate = false;
                    break;
                case FramerateMode.PFR:
                    this.IsPeakFramerate = true;
                    this.ShowPeakFramerate = true;
                    break;
            }

            double cqStep = userSettingService.GetUserSetting<double>(UserSettingConstants.X264Step);
            double rfValue = 0;
            this.SetQualitySliderBounds();
            switch (this.SelectedVideoEncoder)
            {
                case VideoEncoder.FFMpeg:
                case VideoEncoder.FFMpeg2:
                    if (preset.Task.Quality.HasValue)
                    {
                        int cq;
                        int.TryParse(preset.Task.Quality.Value.ToString(CultureInfo.InvariantCulture), out cq);
                        this.RF = 32 - cq;
                    }
                    break;
                case VideoEncoder.VP8:
                    if (preset.Task.Quality.HasValue)
                    {
                        int cq;
                        int.TryParse(preset.Task.Quality.Value.ToString(CultureInfo.InvariantCulture), out cq);
                        this.RF = 63 - cq;
                    }
                    break;
                case VideoEncoder.X265:
                case VideoEncoder.X264:

                    double multiplier = 1.0 / cqStep;
                    if (preset.Task.Quality.HasValue)
                    {
                        rfValue = preset.Task.Quality.Value * multiplier;
                    }

                    this.RF = this.QualityMax - (int)Math.Round(rfValue, 0);

                    break;

                case VideoEncoder.Theora:              
                case VideoEncoder.QuickSync:
                    if (preset.Task.Quality.HasValue)
                    {
                        this.RF = (int)preset.Task.Quality.Value;
                    }
                    break;
            }

            this.Task.TwoPass = preset.Task.TwoPass;
            this.Task.TurboFirstPass = preset.Task.TurboFirstPass;
            this.Task.VideoBitrate = preset.Task.VideoBitrate;

            this.NotifyOfPropertyChange(() => this.Task);

            if (preset.Task != null)
            {
                this.SetEncoder(preset.Task.VideoEncoder);

                // applies to both x264 and QSV
                if (preset.Task.VideoEncoder == VideoEncoder.X264 ||
                    preset.Task.VideoEncoder == VideoEncoder.QuickSync)
                {
                    this.H264Profile = preset.Task.H264Profile;
                    this.H264Level = preset.Task.H264Level;
                }
                else
                {
                    this.H264Profile = x264Profile.None;
                    this.H264Level = "Auto";
                }

                // x264 Only
                if (preset.Task.VideoEncoder == VideoEncoder.X264)
                {
                    this.X264PresetValue = (int)preset.Task.X264Preset;
                    this.X264Tune = preset.Task.X264Tune;
                    this.FastDecode = preset.Task.FastDecode;
                }
                else
                {
                    this.X264PresetValue = (int)x264Preset.Medium;
                    this.X264Tune = x264Tune.None;
                    this.FastDecode = false;
                }

                // x265 Only
                if (preset.Task.VideoEncoder == VideoEncoder.X265)
                {
                    this.X265PresetValue = (int)preset.Task.X264Preset;
                    this.X265Tune = preset.Task.X265Tune;
                    this.H265Profile = preset.Task.H265Profile;
                }
                else
                {
                    this.X265PresetValue = (int)x265Preset.VeryFast;
                    this.X265Tune = x265Tune.None;
                    this.H265Profile = x265Profile.None;
                }

                // QSV Only
                if (preset.Task.VideoEncoder == VideoEncoder.QuickSync)
                {
                    this.QsvPresetValue = (int)preset.Task.QsvPreset;
                }
                else
                {
                    this.QsvPresetValue = SystemInfo.IsHswOrNewer
                                              ? (int)QsvPreset.Quality
                                              : (int)QsvPreset.Balanced;
                }

                this.ExtraArguments = preset.Task.ExtraAdvancedArguments;
                this.UseAdvancedTab = !string.IsNullOrEmpty(preset.Task.AdvancedEncoderOptions) && this.ShowAdvancedTab;
            }
        }

        /// <summary>
        /// Update all the UI controls based on the encode task passed in.
        /// </summary>
        /// <param name="task">
        /// The task.
        /// </param>
        public void UpdateTask(EncodeTask task)
        {
            this.Task = task;
            this.NotifyOfPropertyChange(() => this.IsConstantFramerate);
            this.NotifyOfPropertyChange(() => this.IsConstantQuantity);
            this.NotifyOfPropertyChange(() => this.IsPeakFramerate);
            this.NotifyOfPropertyChange(() => this.IsVariableFramerate);
            this.NotifyOfPropertyChange(() => this.SelectedVideoEncoder);
            this.NotifyOfPropertyChange(() => this.SelectedFramerate);
            this.NotifyOfPropertyChange(() => this.RF);
            this.NotifyOfPropertyChange(() => this.DisplayRF);
            this.NotifyOfPropertyChange(() => this.Task.VideoBitrate);
            this.NotifyOfPropertyChange(() => this.Task.TwoPass);
            this.NotifyOfPropertyChange(() => this.Task.TurboFirstPass);

            this.NotifyOfPropertyChange(() => this.X264Tune);
            this.NotifyOfPropertyChange(() => this.X264Preset);
            this.NotifyOfPropertyChange(() => this.H264Level);
            this.NotifyOfPropertyChange(() => this.H264Profile);
            this.NotifyOfPropertyChange(() => this.FastDecode);
            this.NotifyOfPropertyChange(() => this.ExtraArguments);
            this.NotifyOfPropertyChange(() => this.QsvPreset);

            this.NotifyOfPropertyChange(() => this.H265Profile);
            this.NotifyOfPropertyChange(() => this.X265Preset);
            this.NotifyOfPropertyChange(() => this.X265Tune);
        }

        /// <summary>
        /// Set the currently selected encoder.
        /// </summary>
        /// <param name="encoder">
        /// The Video Encoder.
        /// </param>
        public void SetEncoder(VideoEncoder encoder)
        {
            this.DisplayH264Options = encoder == VideoEncoder.X264 || encoder == VideoEncoder.QuickSync;
            this.DisplayX264Options = encoder == VideoEncoder.X264;
            this.DisplayQSVOptions = encoder == VideoEncoder.QuickSync;
            this.DisplayX265Options = encoder == VideoEncoder.X265;

            if (encoder == VideoEncoder.QuickSync)
            {
                this.UseAdvancedTab = false;
            }
        }

        /// <summary>
        /// Trigger a Notify Property Changed on the Task to force various UI elements to update.
        /// </summary>
        public void RefreshTask()
        {
            this.NotifyOfPropertyChange(() => this.Task);

            if ((Task.OutputFormat == OutputFormat.Mp4) && this.SelectedVideoEncoder == VideoEncoder.Theora)
            {
                this.SelectedVideoEncoder = VideoEncoder.X264;
            }
        }

        /// <summary>
        /// Clear advanced settings.
        /// </summary>
        public void ClearAdvancedSettings()
        {
            this.canClear = false;
            this.X264PresetValue = 5;
            this.X264Tune = x264Tune.None;
            this.H264Profile = x264Profile.None;
            this.FastDecode = false;
            this.H264Level = "Auto";
            this.ExtraArguments = string.Empty;
            this.canClear = true;

            this.H265Profile = x265Profile.None;
            this.x265PresetValue = 1;
            this.X265Tune = x265Tune.None;
        }

        /// <summary>
        /// The copy query.
        /// </summary>
        public void CopyQuery()
        {
            Clipboard.SetDataObject(this.SelectedVideoEncoder == VideoEncoder.X264 ? this.GetActualx264Query() : this.ExtraArguments);
        }

        #endregion

        /// <summary>
        /// Set the bounds of the Constant Quality Slider
        /// </summary>
        private void SetQualitySliderBounds()
        {
            // Note Updating bounds to the same values won't trigger an update.
            // The properties are smart enough to not take in equal values.
            switch (this.SelectedVideoEncoder)
            {
                case VideoEncoder.FFMpeg:
                case VideoEncoder.FFMpeg2:
                    this.QualityMin = 1;
                    this.QualityMax = 31;
                    break;
                case VideoEncoder.QuickSync:
                    this.QualityMin = 0;
                    this.QualityMax = 51;
                    break;
                case VideoEncoder.X264:
                case VideoEncoder.X265:
                    this.QualityMin = 0;
                    this.QualityMax = (int)(51 / userSettingService.GetUserSetting<double>(UserSettingConstants.X264Step));
                    break;
                case VideoEncoder.Theora:
                case VideoEncoder.VP8:
                    this.QualityMin = 0;
                    this.QualityMax = 63;
                    break;
            }
        }

        /// <summary>
        /// Reset advanced tab.
        /// </summary>
        private void ResetAdvancedTab()
        {
            if (canClear)
            {
                this.advancedEncoderOptionsCommand.ExecuteClearAdvanced();
            }
        }

        /// <summary>
        /// The get actualx 264 query.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string GetActualx264Query()
        {
            if (!GeneralUtilities.IsLibHbPresent)
            {
                return string.Empty; // Feature is disabled.
            }

            string preset = EnumHelper<x264Preset>.GetDisplay(this.X264Preset).ToLower().Replace(" ", string.Empty);
            string profile = EnumHelper<x264Profile>.GetDisplay(this.H264Profile).ToLower();

            List<string> tunes = new List<string>();
            if (X264Tune != x264Tune.None)
            {
                tunes.Add(this.X264Tune.ToString().ToLower().Replace(" ", string.Empty)); // TODO tidy this sillyness up.
            }
            if (this.FastDecode)
            {
                tunes.Add("fastdecode");
            }

            // Get the width or height, default if we don't have it yet so we don't crash.
            int width = this.Task.Width.HasValue ? this.Task.Width.Value : 720;
            int height = this.Task.Height.HasValue ? this.Task.Height.Value : 576;

            if (height == 0)
            {
                height = 576;
            }

            if (width == 0)
            {
                width = 720;
            }

            try
            {
                return HandBrakeUtils.CreateX264OptionsString(
                    preset,
                    tunes,
                    this.ExtraArguments,
                    profile,
                    this.H264Level,
                    width,
                    height);
            }
            catch (Exception)
            {
                return "Error: Libhb not loaded.";
            }
        }

        /// <summary>
        /// The user setting service_ setting changed.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void UserSettingServiceSettingChanged(object sender, HandBrake.ApplicationServices.EventArgs.SettingChangedEventArgs e)
        {
            if (e.Key == UserSettingConstants.ShowAdvancedTab)
            {
                this.NotifyOfPropertyChange(() => this.ShowAdvancedTab);
            }
        }
    }
}