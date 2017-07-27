﻿using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Orchard.DisplayManagement.Descriptors;
using Orchard.Environment.Cache;
using Orchard.Templates.Models;
using YesSql;

namespace Orchard.Templates.Services
{
    public class TemplatesManager
    {
        private readonly IShapeTableManager _shapeTableManager;
        private readonly IMemoryCache _memoryCache;
        private readonly ISignal _signal;
        private readonly ISession _session;

        private const string CacheKey = nameof(TemplatesManager);

        public TemplatesManager(IShapeTableManager shapeTableManager,
            IMemoryCache memoryCache, ISignal signal, ISession session)
        {
            _shapeTableManager = shapeTableManager;
            _memoryCache = memoryCache;
            _signal = signal;
            _session = session;
        }

        public IChangeToken ChangeToken => _signal.GetToken(CacheKey);

        /// <inheritdoc/>
        public async Task<TemplatesDocument> GetTemplatesDocumentAsync()
        {
            TemplatesDocument document;

            if (!_memoryCache.TryGetValue(CacheKey, out document))
            {
                document = await _session.Query<TemplatesDocument>().FirstOrDefaultAsync();

                if (document == null)
                {
                    lock (_memoryCache)
                    {
                        if (!_memoryCache.TryGetValue(CacheKey, out document))
                        {
                            document = new TemplatesDocument();

                            _session.Save(document);
                            _memoryCache.Set(CacheKey, document);
                            _signal.SignalToken(CacheKey);
                        }
                    }
                }
                else
                {
                    _memoryCache.Set(CacheKey, document);
                    _signal.SignalToken(CacheKey);
                }
            }

            return document;
        }

        public async Task RemoveTemplateAsync(string name)
        {
            // not from the cache, from a new query not to create a new database record
            var document = await _session.Query<TemplatesDocument>().FirstOrDefaultAsync();
            _signal.SignalToken(_shapeTableManager.GetChangeTokenKey(document.Templates[name].Theme));

            document.Templates.Remove(name);
            _session.Save(document);

            _memoryCache.Set(CacheKey, document);
            _signal.SignalToken(CacheKey);
        }

        public async Task AddTemplateAsync(string name, Template template)
        {
            await UpdateTemplateAsync(name, template);
            _signal.SignalToken(_shapeTableManager.GetChangeTokenKey(template.Theme));
        }

        public async Task UpdateTemplateAsync(string name, Template template)
        {
            // not from the cache, from a new query not to create a new database record
            var document = await _session.Query<TemplatesDocument>().FirstOrDefaultAsync();

            document.Templates[name] = template;
            _session.Save(document);

            _memoryCache.Set(CacheKey, document);
            _signal.SignalToken(CacheKey);
        }
    }
}
