﻿/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2013-2021  Denis Kuzmin <x-3F@outlook.com> github/3F
 * Copyright (c) MvsSln contributors https://github.com/3F/MvsSln/graphs/contributors
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
*/

using System;

namespace net.r_eg.MvsSln.Core
{
    public class RuleOfConfig: IRuleOfConfig
    {
        /// <summary>
        /// Rules of platform names.
        /// details: https://github.com/3F/vsSolutionBuildEvent/issues/14
        ///        + MS Connect Issue #503935
        /// </summary>
        /// <param name="name">Platform name.</param>
        /// <returns></returns>
        public string Platform(string name)
        {
            if(name == null) {
                return null;
            }

            if(String.Compare(name, "Any CPU", StringComparison.OrdinalIgnoreCase) == 0) {
                return "AnyCPU";
            }
            return name;
        }

        /// <summary>
        /// Rules of configuration names.
        /// </summary>
        /// <param name="name">Configuration name.</param>
        /// <returns></returns>
        public string Configuration(string name)
        {
            return name;
        }
    }
}
