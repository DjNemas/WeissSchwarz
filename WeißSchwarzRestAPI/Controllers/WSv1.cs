using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeißSchwarzSharedClasses.DB;
using WeißSchwarzSharedClasses.Models;

namespace WSRestAPI.Controllers
{
    [Route("v1/ws/")]
    [ApiController]
    public class WSv1 : ControllerBase
    {
        private readonly WSContext db;

        /// <summary>
        /// ctor
        /// </summary>
        public WSv1()
        {
            db = new();
        }


        #region Status https://.../v1/ws/
        /// <summary>
        /// Check if API is working
        /// </summary>
        /// <returns>String when working</returns>
        /// <response code="200">Returns a string</response>

        [HttpGet(Name = "GetStatus")]
        [ProducesResponseType(typeof(string), 200)]
        [Produces("text/plain")]
        public IActionResult Get()
        {
            return Ok("WS Rest API is working! :)");
        }
        #endregion

        #region https://.../v1/ws/dataversion

        /// <summary>
        /// Get the current DataVersion.
        /// It increase everytime new data is updated in DB.
        /// </summary>
        /// <returns>Current DataVersion</returns>
        /// <response code="200">Returns a String</response>

        [HttpGet("dataversion", Name = "GetDataVersion")]
        [ProducesResponseType(typeof(string), 200)]
        [Produces("text/plain")]
        public IActionResult GetDataVersion()
        {
            string test = String.Empty;
            try
            {
                test = db.DataVersion.FirstOrDefault(x => x.ID == 1).Version.ToString();
            }
            catch(Exception ex)
            {
                StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
            return Ok(test);
        }

        #endregion

        #region All Sets https://.../v1/ws/sets/all

        /// <summary>
        /// Returns all Sets without Cards
        /// </summary>
        /// <returns>Collection of Sets</returns>
        /// <response code="200">Returns all Sets without Cards</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("sets", Name = "GetAllSets")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Set>), 200)]
        [ProducesResponseType(typeof(string), 500)]

        public async Task<IActionResult> GetAllSets()
        {
            try
            {
                return Ok(await db.Sets.ToListAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns all Sets with Cards
        /// </summary>
        /// <returns>Collection of Sets</returns>
        /// <response code="200">Returns all Sets with Cards</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("sets/cards", Name = "GetAllSetsWithCards")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Set>), 200)]
        [ProducesResponseType(typeof(string), 500)]

        public async Task<IActionResult> GetAllSetsWithCards()
        {
            try
            {
                return Ok(await db.Sets.Include(x => x.Cards).ToListAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns all Sets with Cards and Traits of Cards
        /// </summary>
        /// <returns>Collection of Sets</returns>
        /// <response code="200">Returns all Sets with Cards and Traits of Cards</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("sets/cards/traits", Name = "GetAllSetsWithCardsAndTraits")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Set>), 200)]
        [ProducesResponseType(typeof(string), 500)]

        public async Task<IActionResult> GetAllSetsWithCardsAndTraits()
        {
            try
            {
                return Ok(await db.Sets.Include(x => x.Cards).ThenInclude(x => x.Traits).ToListAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns all Sets with Cards and Triggers of Cards
        /// </summary>
        /// <returns>Collection of Sets</returns>
        /// <response code="200">Returns all Sets with Cards and Triggers of Cards</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("sets/cards/triggers", Name = "GetAllSetsWithCardsAndTriggers")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Set>), 200)]
        [ProducesResponseType(typeof(string), 500)]

        public async Task<IActionResult> GetAllSetsWithCardsAndTriggers()
        {
            try
            {
                return Ok(await db.Sets.Include(x => x.Cards).ThenInclude(x => x.Triggers).ToListAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns all Sets with Cards, Traits and Triggers
        /// </summary>
        /// <returns>Collection of Sets</returns>
        /// <response code="200">Returns all Sets with Cards, Traits and Triggers</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("sets/all", Name = "GetAllSetsWithAll")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Set>), 200)]
        [ProducesResponseType(typeof(string), 500)]

        public async Task<IActionResult> GetAllSetsWithCardsTraitsAndTriggers()
        {
            try
            {
                return Ok(await db.Sets.Include(x => x.Cards).ThenInclude(x => x.Traits)
                    .Include(x => x.Cards).ThenInclude(x => x.Triggers).ToListAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }
        #endregion

        #region Sets by SetID https://.../v1/ws/sets/{setid}/all

        /// <summary>
        /// Returns all Sets by SetID. Like on Cards (Prefix/SetID-CardID)
        /// It can contain multiple Sets. Trial Deck/Booster Pack/Extra Booster etc.
        /// </summary>
        /// <returns>Collection of Sets by SetID</returns>
        /// <param name="setid">The SetID</param>
        /// <response code="200">Returns all Sets of SetID</response>
        /// <response code="400">BadRequest Input was Null or Empty</response>
        /// <response code="404">No Sets found in Database</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("sets/{setid}", Name = "GetSetsBySetID")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Set>), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [ProducesErrorResponseType(typeof(string))]
        public async Task<IActionResult> GetAllSetsBySetID(string setid)
        {
            try
            {
                if (string.IsNullOrEmpty(setid))
                {
                    return BadRequest("Bad Request. Input was Empty");
                }
                List<Set> sets = await db.Sets.Where(x => x.SetID.ToLower() == setid.ToLower()).ToListAsync();
                if (sets.Count() == 0)
                {
                    return NotFound("No Sets found in Database.");
                }
                return Ok(sets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns all Sets by SetID with Cards. Like on Cards (Prefix/SetID-CardID)
        /// It can contain multiple Sets. Trial Deck/Booster Pack/Extra Booster etc.
        /// </summary>
        /// <returns>Collection of Sets by SetID</returns>
        /// <param name="setid">The SetID</param>
        /// <response code="200">Returns all Sets of SetID with Cards</response>
        /// <response code="400">BadRequest Input was Null or Empty</response>
        /// <response code="404">No Sets found in Database</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("sets/{setid}/cards", Name = "GetSetsBySetIDWithCards")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Set>), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [ProducesErrorResponseType(typeof(string))]
        public async Task<IActionResult> GetAllSetsBySetIDWithCards(string setid)
        {
            try
            {
                if (string.IsNullOrEmpty(setid))
                {
                    return BadRequest("Bad Request. Input was Empty");
                }
                List<Set> sets = await db.Sets.Where(x => x.SetID.ToLower() == setid.ToLower()).Include(x => x.Cards).ToListAsync();
                if (sets.Count() == 0)
                {
                    return NotFound("No Sets found in Database.");
                }
                return Ok(sets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns all Sets by SetID with Cards and Traits. Like on Cards (Prefix/SetID-CardID)
        /// It can contain multiple Sets. Trial Deck/Booster Pack/Extra Booster etc.
        /// </summary>
        /// <returns>Collection of Sets by SetID</returns>
        /// <param name="setid">The SetID</param>
        /// <response code="200">Returns all Sets of SetID with Cards and Traits</response>
        /// <response code="400">BadRequest Input was Null or Empty</response>
        /// <response code="404">No Sets found in Database</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("sets/{setid}/cards/traits", Name = "GetSetsBySetIDWithCardsAndTraits")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Set>), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [ProducesErrorResponseType(typeof(string))]
        public async Task<IActionResult> GetAllSetsBySetIDWithCardsAndTraits(string setid)
        {
            try
            {
                if (string.IsNullOrEmpty(setid))
                {
                    return BadRequest("Bad Request. Input was Empty");
                }
                List<Set> sets = await db.Sets
                    .Where(x => x.SetID.ToLower() == setid.ToLower()).Include(x => x.Cards)
                    .ThenInclude(x => x.Traits)
                    .ToListAsync();
                if (sets.Count() == 0)
                {
                    return NotFound("No Sets found in Database.");
                }
                return Ok(sets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns all Sets by SetID with Cards and Triggers. Like on Cards (Prefix/SetID-CardID)
        /// It can contain multiple Sets. Trial Deck/Booster Pack/Extra Booster etc.
        /// </summary>
        /// <returns>Collection of Sets by SetID</returns>
        /// <param name="setid">The SetID</param>
        /// <response code="200">Returns all Sets of SetID with Cards and Triggers</response>
        /// <response code="400">BadRequest Input was Null or Empty</response>
        /// <response code="404">No Sets found in Database</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("sets/{setid}/cards/triggers", Name = "GetSetsBySetIDWithCardsAndTriggers")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Set>), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [ProducesErrorResponseType(typeof(string))]
        public async Task<IActionResult> GetAllSetsBySetIDWithCardsAndTrigger(string setid)
        {
            try
            {
                if (string.IsNullOrEmpty(setid))
                {
                    return BadRequest("Bad Request. Input was Empty");
                }
                List<Set> sets = await db.Sets
                    .Where(x => x.SetID.ToLower() == setid.ToLower()).Include(x => x.Cards)
                    .ThenInclude(x => x.Triggers)
                    .ToListAsync();
                if (sets.Count() == 0)
                {
                    return NotFound("No Sets found in Database.");
                }
                return Ok(sets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns all Sets by SetID with Cards, Traits and Triggers. Like on Cards (Prefix/SetID-CardID)
        /// It can contain multiple Sets. Trial Deck/Booster Pack/Extra Booster etc.
        /// </summary>
        /// <returns>Collection of Sets by SetID</returns>
        /// <param name="setid">The SetID</param>
        /// <response code="200">Returns all Sets of SetID with Cards, Traits and Triggers</response>
        /// <response code="400">BadRequest Input was Null or Empty</response>
        /// <response code="404">No Sets found in Database</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("sets/{setid}/all", Name = "GetSetsBySetIDWithAll")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Set>), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [ProducesErrorResponseType(typeof(string))]
        public async Task<IActionResult> GetAllSetsBySetIDWithCardsTraitsAndTrigger(string setid)
        {
            try
            {
                if (string.IsNullOrEmpty(setid))
                {
                    return BadRequest("Bad Request. Input was Empty");
                }
                List<Set> sets = await db.Sets
                    .Where(x => x.SetID.ToLower() == setid.ToLower()).Include(x => x.Cards)
                    .ThenInclude(x => x.Traits)
                    .Include(x => x.Cards)
                    .ThenInclude(x => x.Triggers)
                    .ToListAsync();
                if (sets.Count() == 0)
                {
                    return NotFound("No Sets found in Database.");
                }
                return Ok(sets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        #endregion

        #region Set by ID https://.../v1/ws/set/{id}/all

        /// <summary>
        /// Returns a specific Sets by ID.
        /// </summary>
        /// <returns>Set by ID</returns>
        /// <param name="id">Unique ID of specific Set</param>
        /// <response code="200">Returns a specific Sets by ID</response>
        /// <response code="400">BadRequest Input was not a Integer</response>
        /// <response code="404">No Sets found in Database</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("set/{id}", Name = "GetSetByID")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Set), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [ProducesErrorResponseType(typeof(string))]
        public async Task<IActionResult> GetSetByID(int id)
        {
            try
            {
                Set sets = await db.Sets.FirstOrDefaultAsync(x => x.ID == id);
                if (sets == null)
                {
                    return NotFound("No Sets found in Database.");
                }
                return Ok(sets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns a specific Sets by ID with Cards.
        /// </summary>
        /// <returns>Set by ID</returns>
        /// <param name="id">Unique ID of specific Set</param>
        /// <response code="200">Returns a specific Sets by ID with Cards</response>
        /// <response code="400">BadRequest Input was not a Integer</response>
        /// <response code="404">No Sets found in Database</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("set/{id}/cards", Name = "GetSetByIDWithCards")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Set), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [ProducesErrorResponseType(typeof(string))]
        public async Task<IActionResult> GetSetByIDWithCards(int id)
        {
            try
            {
                Set sets = await db.Sets
                    .Include(x => x.Cards)
                    .FirstOrDefaultAsync(x => x.ID == id);
                if (sets == null)
                {
                    return NotFound("No Sets found in Database.");
                }
                return Ok(sets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns a specific Sets by ID with Cards and Traits
        /// </summary>
        /// <returns>Set by ID</returns>
        /// <param name="id">Unique ID of specific Set</param>
        /// <response code="200">Returns a specific Sets by ID with Cards and Traits</response>
        /// <response code="400">BadRequest Input was not a Integer</response>
        /// <response code="404">No Sets found in Database</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("set/{id}/cards/traits", Name = "GetSetByIDWithCardsAndTraits")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Set), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [ProducesErrorResponseType(typeof(string))]
        public async Task<IActionResult> GetSetByIDWithCardsAndTraits(int id)
        {
            try
            {
                Set sets = await db.Sets
                    .Include(x => x.Cards)
                    .ThenInclude(x => x.Traits)
                    .FirstOrDefaultAsync(x => x.ID == id);
                if (sets == null)
                {
                    return NotFound("No Sets found in Database.");
                }
                return Ok(sets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns a specific Sets by ID with Cards and Triggers
        /// </summary>
        /// <returns>Set by ID</returns>
        /// <param name="id">Unique ID of specific Set</param>
        /// <response code="200">Returns a specific Sets by ID with Cards and Triggers</response>
        /// <response code="400">BadRequest Input was not a Integer</response>
        /// <response code="404">No Sets found in Database</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("set/{id}/cards/triggers", Name = "GetSetByIDWithCardsAndTriggers")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Set), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [ProducesErrorResponseType(typeof(string))]
        public async Task<IActionResult> GetSetByIDWithCardsAndTriggers(int id)
        {
            try
            {
                Set sets = await db.Sets
                    .Include(x => x.Cards)
                    .ThenInclude(x => x.Triggers)
                    .FirstOrDefaultAsync(x => x.ID == id);
                if (sets == null)
                {
                    return NotFound("No Sets found in Database.");
                }
                return Ok(sets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns a specific Sets by ID with Cards,Traits and Triggers
        /// </summary>
        /// <returns>Set by ID</returns>
        /// <param name="id">Unique ID of specific Set</param>
        /// <response code="200">Returns a specific Sets by ID with Cards,Traits and Triggers</response>
        /// <response code="400">BadRequest Input was not a Integer</response>
        /// <response code="404">No Sets found in Database</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("set/{id}/all", Name = "GetSetByIDWithAll")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Set), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [ProducesErrorResponseType(typeof(string))]
        public async Task<IActionResult> GetSetByIDWithCardsTraitsAndTriggers(int id)
        {
            try
            {
                Set sets = await db.Sets
                    .Include(x => x.Cards)
                    .ThenInclude(x => x.Traits)
                    .Include(x => x.Cards)
                    .ThenInclude(x => x.Triggers)
                    .FirstOrDefaultAsync(x => x.ID == id);
                if (sets == null)
                {
                    return NotFound("No Sets found in Database.");
                }
                return Ok(sets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        #endregion

        #region Cards https://.../v1/ws/cards/all

        /// <summary>
        /// Returns all Cards
        /// </summary>
        /// <returns>Collection of Sets</returns>
        /// <response code="200">Returns all Cards</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("cards", Name = "GetAllCards")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Card>), 200)]
        [ProducesResponseType(typeof(string), 500)]

        public async Task<IActionResult> GetAllCards()
        {
            try
            {
                return Ok(await db.Cards.ToListAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns all Cards with Traits
        /// </summary>
        /// <returns>Collection of Sets</returns>
        /// <response code="200">Returns all Cards with Traits</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("cards/traits", Name = "GetAllCardsWithTraits")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Card>), 200)]
        [ProducesResponseType(typeof(string), 500)]

        public async Task<IActionResult> GetAllCardsWithTraits()
        {
            try
            {
                return Ok(await db.Cards.Include(x => x.Traits).ToListAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns all Cards with Triggers
        /// </summary>
        /// <returns>Collection of Sets</returns>
        /// <response code="200">Returns all Cards with Triggers</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("cards/triggers", Name = "GetAllCardsWithTriggers")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Card>), 200)]
        [ProducesResponseType(typeof(string), 500)]

        public async Task<IActionResult> GetAllCardsWithTriggers()
        {
            try
            {
                return Ok(await db.Cards
                    .Include(x => x.Triggers)
                    .ToListAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns all Cards with Traits and Trigger
        /// </summary>
        /// <returns>Collection of Sets</returns>
        /// <response code="200">Returns all Cards with Traits and Trigger</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("cards/all", Name = "GetAllCardsWithAll")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Card>), 200)]
        [ProducesResponseType(typeof(string), 500)]

        public async Task<IActionResult> GetAllCardsWithTraitsAndTriggers()
        {
            try
            {
                return Ok(await db.Cards
                    .Include(x => x.Traits)
                    .Include(x => x.Triggers)
                    .ToListAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        #endregion

        #region Card https://.../v1/ws/card/{longid}/all

        /// <summary>
        /// Returns a Card by LongID (Prefix/SetID-CardID)
        /// This is a Unquie ID
        /// </summary>
        /// <returns>Card by LongID</returns>
        /// <param name="prefix">Prefix of specific Card</param>
        /// <param name="setid">SetID of specific Card</param>
        /// <param name="cardid">CardID of specific Card</param>
        /// <response code="200">Returns a Card by LongID</response>
        /// <response code="400">BadRequest Input was Null or Empty</response>
        /// <response code="404">No Sets found in Database</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("card/{prefix}/{setid}/{cardid}", Name = "GetCardByLongID")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Card), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [ProducesErrorResponseType(typeof(string))]
        public async Task<IActionResult> GetCardByID(string prefix, string setid, string cardid)
        {
            try
            {
                
                if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(setid) || string.IsNullOrEmpty(cardid))
                {
                    return BadRequest("Bad Request. Input was Empty");
                }
                string longid = prefix + "/" + setid + "-" + cardid;
                Card card = await db.Cards
                    .FirstOrDefaultAsync(x => x.LongID.ToLower() == longid.ToLower());
                if (card == null)
                {
                    return NotFound("No Sets found in Database.");
                }
                return Ok(card);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns a Card by LongID with Traits (Prefix/SetID-CardID)
        /// This is a Unquie ID
        /// </summary>
        /// <returns>Card by LongID</returns>
        /// <param name="prefix">Prefix of specific Card</param>
        /// <param name="setid">SetID of specific Card</param>
        /// <param name="cardid">CardID of specific Card</param>
        /// <response code="200">Returns a Card by LongID with Traits</response>
        /// <response code="400">BadRequest Input was Null or Empty</response>
        /// <response code="404">No Sets found in Database</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("card/{prefix}/{setid}/{cardid}/traits", Name = "GetCardByLongIDWithTraits")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Card), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [ProducesErrorResponseType(typeof(string))]
        public async Task<IActionResult> GetCardByIDWithTraits(string prefix, string setid, string cardid)
        {
            try
            {
                if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(setid) || string.IsNullOrEmpty(cardid))
                {
                    return BadRequest("Bad Request. Input was Empty");
                }
                string longid = prefix + "/" + setid + "-" + cardid;
                Card card = await db.Cards
                    .Include(x => x.Traits)
                    .FirstOrDefaultAsync(x => x.LongID.ToLower() == longid.ToLower());
                if (card == null)
                {
                    return NotFound("No Sets found in Database.");
                }
                return Ok(card);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns a Card by LongID with Triggers (Prefix/SetID-CardID)
        /// This is a Unquie ID
        /// </summary>
        /// <returns>Card by LongID</returns>
        /// <param name="prefix">Prefix of specific Card</param>
        /// <param name="setid">SetID of specific Card</param>
        /// <param name="cardid">CardID of specific Card</param>
        /// <response code="200">Returns a Card by LongID with Triggers</response>
        /// <response code="400">BadRequest Input was Null or Empty</response>
        /// <response code="404">No Sets found in Database</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("card/{prefix}/{setid}/{cardid}/triggers", Name = "GetCardByLongIDWithTriggers")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Card), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [ProducesErrorResponseType(typeof(string))]
        public async Task<IActionResult> GetCardByIDWithTriggers(string prefix, string setid, string cardid)
        {
            try
            {
                if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(setid) || string.IsNullOrEmpty(cardid))
                {
                    return BadRequest("Bad Request. Input was Empty");
                }
                string longid = prefix + "/" + setid + "-" + cardid;
                Card card = await db.Cards
                    .Include(x => x.Triggers)
                    .FirstOrDefaultAsync(x => x.LongID.ToLower() == longid.ToLower());
                if (card == null)
                {
                    return NotFound("No Sets found in Database.");
                }
                return Ok(card);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }

        /// <summary>
        /// Returns a Card by LongID with Traits and Triggers (Prefix/SetID-CardID)
        /// This is a Unquie ID
        /// </summary>
        /// <returns>Card by LongID</returns>
        /// <param name="prefix">Prefix of specific Card</param>
        /// <param name="setid">SetID of specific Card</param>
        /// <param name="cardid">CardID of specific Card</param>
        /// <response code="200">Returns a Card by LongID with Traits and Triggers</response>
        /// <response code="400">BadRequest Input was Null or Empty</response>
        /// <response code="404">No Sets found in Database</response>
        /// <response code="500">Internal Error with Exception.Message and Exception.InnerMessage</response>
        [HttpGet("card/{prefix}/{setid}/{cardid}/all", Name = "GetCardByLongIDWithAll")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Card), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [ProducesErrorResponseType(typeof(string))]
        public async Task<IActionResult> GetCardByIDWithTraitsAndTriggers(string prefix, string setid, string cardid)
        {
            try
            {
                if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(setid) || string.IsNullOrEmpty(cardid))
                {
                    return BadRequest("Bad Request. Input was Empty");
                }
                string longid = prefix + "/" + setid + "-" + cardid;
                Card card = await db.Cards
                    .Include(x => x.Traits)
                    .Include(x => x.Triggers)
                    .FirstOrDefaultAsync(x => x.LongID.ToLower() == longid.ToLower());
                if (card == null)
                {
                    return NotFound("No Sets found in Database.");
                }
                return Ok(card);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error occured:\n" + ex.Message + " Inner Exception:\n" + ex.InnerException);
            }
        }
        #endregion

    }
}
